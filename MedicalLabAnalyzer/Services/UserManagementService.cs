using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using System.Security.Cryptography;
using System.Text;

namespace MedicalLabAnalyzer.Services
{
    public class UserManagementService
    {
        private readonly ILogger<UserManagementService> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        private readonly PermissionService _permissionService;
        
        public UserManagementService(
            ILogger<UserManagementService> logger,
            DatabaseService dbService,
            AuditService auditService,
            PermissionService permissionService)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            _permissionService = permissionService;
        }
        
        #region User Management
        
        /// <summary>
        /// إضافة مستخدم جديد
        /// </summary>
        public async Task<User?> CreateUserAsync(User newUser, string password, int createdByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ManageUsers))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ManageUsers, "إنشاء مستخدم جديد");
                    return null;
                }
                
                // التحقق من عدم وجود اسم المستخدم
                var existingUser = await _dbService.GetUserByUsernameAsync(newUser.Username);
                if (existingUser != null)
                {
                    _logger.LogWarning("محاولة إنشاء مستخدم بنفس اسم المستخدم الموجود: {Username}", newUser.Username);
                    return null;
                }
                
                // تشفير كلمة المرور
                newUser.PasswordHash = HashPassword(password);
                newUser.CreatedAt = DateTime.UtcNow;
                newUser.CreatedByUserId = createdByUserId;
                newUser.IsActive = true;
                
                // حفظ في قاعدة البيانات
                var savedUser = await _dbService.AddUserAsync(newUser);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم إنشاء مستخدم جديد: {savedUser.FullName} ({savedUser.Username}) - الدور: {savedUser.Role}",
                    AuditActionType.Create,
                    createdByUserId,
                    $"User created: ID {savedUser.UserId}, Role: {savedUser.Role}"
                );
                
                _logger.LogInformation("تم إنشاء مستخدم جديد {UserId} - {Username}", savedUser.UserId, savedUser.Username);
                return savedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء المستخدم {Username}", newUser.Username);
                return null;
            }
        }
        
        /// <summary>
        /// تحديث بيانات المستخدم
        /// </summary>
        public async Task<User?> UpdateUserAsync(User updatedUser, int updatedByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ManageUsers))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ManageUsers, "تحديث بيانات المستخدم");
                    return null;
                }
                
                var existingUser = await _dbService.GetUserByIdAsync(updatedUser.UserId);
                if (existingUser == null)
                {
                    _logger.LogWarning("محاولة تحديث مستخدم غير موجود: {UserId}", updatedUser.UserId);
                    return null;
                }
                
                // تحديث البيانات
                existingUser.FullName = updatedUser.FullName;
                existingUser.Email = updatedUser.Email;
                existingUser.PhoneNumber = updatedUser.PhoneNumber;
                existingUser.Role = updatedUser.Role;
                existingUser.IsActive = updatedUser.IsActive;
                existingUser.UpdatedAt = DateTime.UtcNow;
                existingUser.UpdatedByUserId = updatedByUserId;
                
                // حفظ التحديثات
                var savedUser = await _dbService.UpdateUserAsync(existingUser);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم تحديث بيانات المستخدم: {savedUser.FullName} ({savedUser.Username})",
                    AuditActionType.Update,
                    updatedByUserId,
                    $"User updated: ID {savedUser.UserId}"
                );
                
                _logger.LogInformation("تم تحديث المستخدم {UserId} - {Username}", savedUser.UserId, savedUser.Username);
                return savedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث المستخدم {UserId}", updatedUser.UserId);
                return null;
            }
        }
        
        /// <summary>
        /// حذف المستخدم (إلغاء تفعيل)
        /// </summary>
        public async Task<bool> DeactivateUserAsync(int userId, int deactivatedByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ManageUsers))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ManageUsers, "إلغاء تفعيل المستخدم");
                    return false;
                }
                
                var user = await _dbService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("محاولة إلغاء تفعيل مستخدم غير موجود: {UserId}", userId);
                    return false;
                }
                
                // منع حذف النفس
                if (userId == deactivatedByUserId)
                {
                    _logger.LogWarning("محاولة المستخدم {UserId} إلغاء تفعيل نفسه", userId);
                    return false;
                }
                
                // إلغاء التفعيل
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedByUserId = deactivatedByUserId;
                
                await _dbService.UpdateUserAsync(user);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم إلغاء تفعيل المستخدم: {user.FullName} ({user.Username})",
                    AuditActionType.Delete,
                    deactivatedByUserId,
                    $"User deactivated: ID {userId}"
                );
                
                _logger.LogInformation("تم إلغاء تفعيل المستخدم {UserId} - {Username}", userId, user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إلغاء تفعيل المستخدم {UserId}", userId);
                return false;
            }
        }
        
        /// <summary>
        /// تفعيل المستخدم
        /// </summary>
        public async Task<bool> ActivateUserAsync(int userId, int activatedByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ManageUsers))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ManageUsers, "تفعيل المستخدم");
                    return false;
                }
                
                var user = await _dbService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("محاولة تفعيل مستخدم غير موجود: {UserId}", userId);
                    return false;
                }
                
                // التفعيل
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedByUserId = activatedByUserId;
                
                await _dbService.UpdateUserAsync(user);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم تفعيل المستخدم: {user.FullName} ({user.Username})",
                    AuditActionType.Update,
                    activatedByUserId,
                    $"User activated: ID {userId}"
                );
                
                _logger.LogInformation("تم تفعيل المستخدم {UserId} - {Username}", userId, user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تفعيل المستخدم {UserId}", userId);
                return false;
            }
        }
        
        /// <summary>
        /// إعادة تعيين كلمة المرور
        /// </summary>
        public async Task<string?> ResetUserPasswordAsync(int userId, int resetByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ResetPasswords))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ResetPasswords, "إعادة تعيين كلمة المرور");
                    return null;
                }
                
                var user = await _dbService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("محاولة إعادة تعيين كلمة مرور لمستخدم غير موجود: {UserId}", userId);
                    return null;
                }
                
                // إنشاء كلمة مرور جديدة
                var newPassword = GenerateRandomPassword();
                var newPasswordHash = HashPassword(newPassword);
                
                // تحديث كلمة المرور
                user.PasswordHash = newPasswordHash;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedByUserId = resetByUserId;
                user.ForcePasswordChange = true; // إجبار تغيير كلمة المرور في أول تسجيل دخول
                
                await _dbService.UpdateUserAsync(user);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم إعادة تعيين كلمة مرور للمستخدم: {user.FullName} ({user.Username})",
                    AuditActionType.PasswordReset,
                    resetByUserId,
                    $"Password reset for user ID {userId}"
                );
                
                _logger.LogInformation("تم إعادة تعيين كلمة المرور للمستخدم {UserId} - {Username}", userId, user.Username);
                return newPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إعادة تعيين كلمة المرور للمستخدم {UserId}", userId);
                return null;
            }
        }
        
        /// <summary>
        /// تغيير دور المستخدم
        /// </summary>
        public async Task<bool> ChangeUserRoleAsync(int userId, UserRole newRole, int changedByUserId)
        {
            try
            {
                // التحقق من الصلاحية
                if (!_permissionService.HasPermission(Permission.ManageUsers))
                {
                    await _permissionService.LogPermissionDeniedAsync(Permission.ManageUsers, "تغيير دور المستخدم");
                    return false;
                }
                
                var user = await _dbService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("محاولة تغيير دور مستخدم غير موجود: {UserId}", userId);
                    return false;
                }
                
                var oldRole = user.Role;
                
                // تحديث الدور
                user.Role = newRole;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedByUserId = changedByUserId;
                
                await _dbService.UpdateUserAsync(user);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم تغيير دور المستخدم {user.FullName} من {oldRole} إلى {newRole}",
                    AuditActionType.Update,
                    changedByUserId,
                    $"Role changed for user ID {userId}: {oldRole} -> {newRole}"
                );
                
                _logger.LogInformation("تم تغيير دور المستخدم {UserId} من {OldRole} إلى {NewRole}", userId, oldRole, newRole);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تغيير دور المستخدم {UserId}", userId);
                return false;
            }
        }
        
        #endregion
        
        #region Bulk Operations
        
        /// <summary>
        /// إنشاء المستخدمين الافتراضيين
        /// </summary>
        public async Task<bool> CreateDefaultUsersAsync()
        {
            try
            {
                _logger.LogInformation("بدء إنشاء المستخدمين الافتراضيين");
                
                var defaultUsers = new[]
                {
                    new { Username = "admin", Password = "admin", FullName = "مدير النظام", Role = UserRole.Manager, Email = "admin@medicallab.com" },
                    new { Username = "lab", Password = "123", FullName = "فني المختبر", Role = UserRole.LabTechnician, Email = "lab@medicallab.com" },
                    new { Username = "reception", Password = "123", FullName = "موظف الاستقبال", Role = UserRole.Receptionist, Email = "reception@medicallab.com" }
                };
                
                foreach (var userData in defaultUsers)
                {
                    // التحقق من وجود المستخدم
                    var existingUser = await _dbService.GetUserByUsernameAsync(userData.Username);
                    if (existingUser != null) continue;
                    
                    var newUser = new User
                    {
                        Username = userData.Username,
                        PasswordHash = HashPassword(userData.Password),
                        FullName = userData.FullName,
                        Role = userData.Role,
                        Email = userData.Email,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = 0 // النظام
                    };
                    
                    await _dbService.AddUserAsync(newUser);
                    
                    _logger.LogInformation("تم إنشاء المستخدم الافتراضي: {Username} - {Role}", userData.Username, userData.Role);
                }
                
                await _auditService.LogAsync(
                    "تم إنشاء المستخدمين الافتراضيين",
                    AuditActionType.SystemInit,
                    null,
                    "Default users created during system initialization"
                );
                
                _logger.LogInformation("تم إكمال إنشاء المستخدمين الافتراضيين");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء المستخدمين الافتراضيين");
                return false;
            }
        }
        
        /// <summary>
        /// الحصول على إحصائيات المستخدمين
        /// </summary>
        public async Task<UserStatistics> GetUserStatisticsAsync()
        {
            try
            {
                var allUsers = await _dbService.GetAllUsersAsync();
                
                return new UserStatistics
                {
                    TotalUsers = allUsers.Count,
                    ActiveUsers = allUsers.Count(u => u.IsActive),
                    InactiveUsers = allUsers.Count(u => !u.IsActive),
                    Managers = allUsers.Count(u => u.Role == UserRole.Manager),
                    LabTechnicians = allUsers.Count(u => u.Role == UserRole.LabTechnician),
                    Receptionists = allUsers.Count(u => u.Role == UserRole.Receptionist),
                    UsersCreatedToday = allUsers.Count(u => u.CreatedAt.Date == DateTime.Today),
                    UsersLoggedInToday = allUsers.Count(u => u.LastLoginAt?.Date == DateTime.Today)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الحصول على إحصائيات المستخدمين");
                return new UserStatistics();
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// التحقق من صحة بيانات المستخدم
        /// </summary>
        public ValidationResult ValidateUser(User user, string? password = null)
        {
            var errors = new List<string>();
            
            // التحقق من اسم المستخدم
            if (string.IsNullOrWhiteSpace(user.Username))
                errors.Add("اسم المستخدم مطلوب");
            else if (user.Username.Length < 3)
                errors.Add("اسم المستخدم يجب أن يكون 3 أحرف على الأقل");
            else if (user.Username.Length > 50)
                errors.Add("اسم المستخدم لا يمكن أن يتجاوز 50 حرف");
            
            // التحقق من الاسم الكامل
            if (string.IsNullOrWhiteSpace(user.FullName))
                errors.Add("الاسم الكامل مطلوب");
            else if (user.FullName.Length > 100)
                errors.Add("الاسم الكامل لا يمكن أن يتجاوز 100 حرف");
            
            // التحقق من البريد الإلكتروني
            if (!string.IsNullOrWhiteSpace(user.Email) && !IsValidEmail(user.Email))
                errors.Add("البريد الإلكتروني غير صحيح");
            
            // التحقق من كلمة المرور (إذا تم توفيرها)
            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 6)
                    errors.Add("كلمة المرور يجب أن تكون 6 أحرف على الأقل");
                else if (password.Length > 100)
                    errors.Add("كلمة المرور لا يمكن أن تتجاوز 100 حرف");
            }
            
            // التحقق من الدور
            if (!Enum.IsDefined(typeof(UserRole), user.Role))
                errors.Add("دور المستخدم غير صحيح");
            
            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }
        
        #endregion
        
        #region Private Methods
        
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + "MedicalLabAnalyzer_Salt_2024";
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }
        
        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int Managers { get; set; }
        public int LabTechnicians { get; set; }
        public int Receptionists { get; set; }
        public int UsersCreatedToday { get; set; }
        public int UsersLoggedInToday { get; set; }
    }
    
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        
        public string ErrorMessage => string.Join(Environment.NewLine, Errors);
    }
    
    #endregion
}