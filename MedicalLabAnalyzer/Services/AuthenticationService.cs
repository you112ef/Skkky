using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.Services
{
    public class AuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        private User? _currentUser;
        private readonly Dictionary<string, DateTime> _loginAttempts;
        private readonly int _maxLoginAttempts = 5;
        private readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);
        
        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            DatabaseService dbService,
            AuditService auditService)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            _loginAttempts = new Dictionary<string, DateTime>();
        }
        
        #region Properties
        
        public User? CurrentUser => _currentUser;
        
        public bool IsAuthenticated => _currentUser != null;
        
        public UserRole? CurrentUserRole => _currentUser?.Role;
        
        public string CurrentUserName => _currentUser?.FullName ?? "غير مسجل";
        
        #endregion
        
        #region Events
        
        public event EventHandler<UserAuthenticatedEventArgs>? UserAuthenticated;
        public event EventHandler<UserLoggedOutEventArgs>? UserLoggedOut;
        public event EventHandler<AuthenticationFailedEventArgs>? AuthenticationFailed;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// تسجيل دخول المستخدم
        /// </summary>
        public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("محاولة تسجيل دخول للمستخدم: {Username}", username);
                
                // التحقق من حالة القفل
                if (IsUserLockedOut(username))
                {
                    var lockoutTime = GetLockoutRemainingTime(username);
                    var result = AuthenticationResult.Failed(
                        $"الحساب مقفل مؤقتاً. المحاولة مرة أخرى بعد {lockoutTime.Minutes} دقيقة"
                    );
                    
                    await _auditService.LogAsync(
                        $"محاولة دخول لحساب مقفل: {username}",
                        AuditActionType.LoginFailed,
                        null,
                        $"Account locked - remaining time: {lockoutTime.Minutes} minutes"
                    );
                    
                    AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs(username, result.ErrorMessage));
                    return result;
                }
                
                // التحقق من بيانات المستخدم
                var user = await _dbService.GetUserByUsernameAsync(username);
                
                if (user == null || !VerifyPassword(password, user.PasswordHash))
                {
                    // تسجيل محاولة فاشلة
                    RecordFailedLoginAttempt(username);
                    
                    await _auditService.LogAsync(
                        $"فشل تسجيل دخول - بيانات خاطئة: {username}",
                        AuditActionType.LoginFailed,
                        null,
                        $"Invalid credentials for username: {username}"
                    );
                    
                    var errorMsg = "اسم المستخدم أو كلمة المرور غير صحيحة";
                    AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs(username, errorMsg));
                    return AuthenticationResult.Failed(errorMsg);
                }
                
                // التحقق من حالة المستخدم
                if (!user.IsActive)
                {
                    await _auditService.LogAsync(
                        $"محاولة دخول لحساب معطل: {username}",
                        AuditActionType.LoginFailed,
                        user.UserId,
                        "Account disabled"
                    );
                    
                    var errorMsg = "الحساب معطل. الرجاء الاتصال بالمدير";
                    AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs(username, errorMsg));
                    return AuthenticationResult.Failed(errorMsg);
                }
                
                // نجح تسجيل الدخول
                _currentUser = user;
                ClearFailedLoginAttempts(username);
                
                // تحديث آخر تسجيل دخول
                await _dbService.UpdateLastLoginAsync(user.UserId);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"نجح تسجيل دخول المستخدم: {user.FullName}",
                    AuditActionType.Login,
                    user.UserId,
                    $"User logged in - Role: {user.Role}"
                );
                
                _logger.LogInformation("نجح تسجيل الدخول للمستخدم {UserId} - {FullName}", user.UserId, user.FullName);
                
                UserAuthenticated?.Invoke(this, new UserAuthenticatedEventArgs(user));
                
                return AuthenticationResult.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل الدخول للمستخدم: {Username}", username);
                
                await _auditService.LogAsync(
                    $"خطأ في تسجيل الدخول: {ex.Message}",
                    AuditActionType.Error,
                    null,
                    $"Authentication error for {username}: {ex.Message}"
                );
                
                var errorMsg = "حدث خطأ في النظام. الرجاء المحاولة مرة أخرى";
                AuthenticationFailed?.Invoke(this, new AuthenticationFailedEventArgs(username, errorMsg));
                return AuthenticationResult.Failed(errorMsg);
            }
        }
        
        /// <summary>
        /// تسجيل خروج المستخدم
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                if (_currentUser != null)
                {
                    var user = _currentUser;
                    
                    await _auditService.LogAsync(
                        $"تسجيل خروج المستخدم: {user.FullName}",
                        AuditActionType.Logout,
                        user.UserId,
                        "User logged out"
                    );
                    
                    _logger.LogInformation("تسجيل خروج للمستخدم {UserId}", user.UserId);
                    
                    UserLoggedOut?.Invoke(this, new UserLoggedOutEventArgs(user));
                    
                    _currentUser = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل الخروج");
            }
        }
        
        /// <summary>
        /// التحقق من الصلاحية
        /// </summary>
        public bool HasPermission(UserRole requiredRole)
        {
            if (_currentUser == null || !_currentUser.IsActive)
                return false;
            
            // المدير يملك كل الصلاحيات
            if (_currentUser.Role == UserRole.Manager)
                return true;
            
            // التحقق من الصلاحية المطلوبة
            return _currentUser.Role == requiredRole || requiredRole == UserRole.All;
        }
        
        /// <summary>
        /// التحقق من صلاحيات متعددة
        /// </summary>
        public bool HasAnyPermission(params UserRole[] roles)
        {
            if (_currentUser == null || !_currentUser.IsActive)
                return false;
            
            if (_currentUser.Role == UserRole.Manager)
                return true;
            
            return roles.Contains(_currentUser.Role) || roles.Contains(UserRole.All);
        }
        
        /// <summary>
        /// تغيير كلمة المرور
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (_currentUser == null)
                    return false;
                
                // التحقق من كلمة المرور الحالية
                if (!VerifyPassword(currentPassword, _currentUser.PasswordHash))
                {
                    await _auditService.LogAsync(
                        "محاولة تغيير كلمة مرور بكلمة مرور حالية خاطئة",
                        AuditActionType.PasswordChangeFailed,
                        _currentUser.UserId,
                        "Incorrect current password"
                    );
                    return false;
                }
                
                // تشفير كلمة المرور الجديدة
                var newPasswordHash = HashPassword(newPassword);
                
                // تحديث في قاعدة البيانات
                await _dbService.UpdateUserPasswordAsync(_currentUser.UserId, newPasswordHash);
                
                // تحديث في الكائن الحالي
                _currentUser.PasswordHash = newPasswordHash;
                
                await _auditService.LogAsync(
                    "تم تغيير كلمة المرور بنجاح",
                    AuditActionType.PasswordChanged,
                    _currentUser.UserId,
                    "Password changed successfully"
                );
                
                _logger.LogInformation("تم تغيير كلمة المرور للمستخدم {UserId}", _currentUser.UserId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تغيير كلمة المرور للمستخدم {UserId}", _currentUser?.UserId);
                return false;
            }
        }
        
        /// <summary>
        /// إعادة تعيين كلمة المرور (للمدير فقط)
        /// </summary>
        public async Task<string?> ResetPasswordAsync(int userId)
        {
            try
            {
                if (!HasPermission(UserRole.Manager))
                {
                    _logger.LogWarning("محاولة إعادة تعيين كلمة مرور بدون صلاحية من المستخدم {CurrentUserId}", _currentUser?.UserId);
                    return null;
                }
                
                // إنشاء كلمة مرور جديدة عشوائية
                var newPassword = GenerateRandomPassword();
                var newPasswordHash = HashPassword(newPassword);
                
                // تحديث في قاعدة البيانات
                await _dbService.UpdateUserPasswordAsync(userId, newPasswordHash);
                
                await _auditService.LogAsync(
                    $"تم إعادة تعيين كلمة مرور للمستخدم {userId}",
                    AuditActionType.PasswordReset,
                    _currentUser?.UserId,
                    $"Password reset for user {userId}"
                );
                
                _logger.LogInformation("تم إعادة تعيين كلمة المرور للمستخدم {UserId}", userId);
                return newPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إعادة تعيين كلمة المرور للمستخدم {UserId}", userId);
                return null;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private bool IsUserLockedOut(string username)
        {
            if (!_loginAttempts.ContainsKey(username))
                return false;
            
            var lockoutTime = _loginAttempts[username];
            return DateTime.Now < lockoutTime.Add(_lockoutDuration);
        }
        
        private TimeSpan GetLockoutRemainingTime(string username)
        {
            if (!_loginAttempts.ContainsKey(username))
                return TimeSpan.Zero;
            
            var lockoutEnd = _loginAttempts[username].Add(_lockoutDuration);
            var remaining = lockoutEnd - DateTime.Now;
            
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        
        private void RecordFailedLoginAttempt(string username)
        {
            var attempts = GetFailedLoginCount(username) + 1;
            
            if (attempts >= _maxLoginAttempts)
            {
                // قفل الحساب
                _loginAttempts[username] = DateTime.Now;
                _logger.LogWarning("تم قفل الحساب {Username} بعد {Attempts} محاولة فاشلة", username, attempts);
            }
            else
            {
                _loginAttempts[username + "_count"] = DateTime.Now;
            }
        }
        
        private int GetFailedLoginCount(string username)
        {
            var countKey = username + "_count";
            if (!_loginAttempts.ContainsKey(countKey))
                return 0;
            
            // حساب المحاولات في آخر ساعة
            var lastAttempt = _loginAttempts[countKey];
            if (DateTime.Now - lastAttempt > TimeSpan.FromHours(1))
            {
                _loginAttempts.Remove(countKey);
                return 0;
            }
            
            return _loginAttempts.Count(kv => kv.Key.StartsWith(username) && kv.Key != username);
        }
        
        private void ClearFailedLoginAttempts(string username)
        {
            var keysToRemove = _loginAttempts.Keys.Where(k => k.StartsWith(username)).ToList();
            foreach (var key in keysToRemove)
            {
                _loginAttempts.Remove(key);
            }
        }
        
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            
            // إضافة salt للأمان
            var saltedPassword = password + "MedicalLabAnalyzer_Salt_2024";
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            
            return Convert.ToBase64String(hashedBytes);
        }
        
        private bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }
        
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    public class AuthenticationResult
    {
        public bool IsSuccess { get; private set; }
        public User? User { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        
        private AuthenticationResult() { }
        
        public static AuthenticationResult Success(User user)
        {
            return new AuthenticationResult
            {
                IsSuccess = true,
                User = user
            };
        }
        
        public static AuthenticationResult Failed(string errorMessage)
        {
            return new AuthenticationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
    
    #endregion
    
    #region Event Args
    
    public class UserAuthenticatedEventArgs : EventArgs
    {
        public User User { get; }
        
        public UserAuthenticatedEventArgs(User user)
        {
            User = user;
        }
    }
    
    public class UserLoggedOutEventArgs : EventArgs
    {
        public User User { get; }
        
        public UserLoggedOutEventArgs(User user)
        {
            User = user;
        }
    }
    
    public class AuthenticationFailedEventArgs : EventArgs
    {
        public string Username { get; }
        public string Reason { get; }
        
        public AuthenticationFailedEventArgs(string username, string reason)
        {
            Username = username;
            Reason = reason;
        }
    }
    
    #endregion
}