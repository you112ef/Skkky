using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly ILogger<LoginViewModel> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoginEnabled = true;
        private bool _isLoggingIn = false;
        private string _errorMessage = string.Empty;
        private string _statusMessage = string.Empty;
        
        public LoginViewModel(
            ILogger<LoginViewModel> logger,
            DatabaseService dbService,
            AuditService auditService)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            
            LoginCommand = new AsyncRelayCommand(LoginAsync, () => CanLogin);
            ClearErrorCommand = new RelayCommand(ClearError);
            
            StatusMessage = "أدخل بيانات المستخدم لتسجيل الدخول";
        }
        
        #region Properties
        
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value, UpdateCanLogin);
        }
        
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value, UpdateCanLogin);
        }
        
        public bool IsLoginEnabled
        {
            get => _isLoginEnabled;
            set => SetProperty(ref _isLoginEnabled, value);
        }
        
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value, UpdateCanLogin);
        }
        
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public bool CanLogin => !IsLoggingIn && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        
        #endregion
        
        #region Commands
        
        public ICommand LoginCommand { get; }
        public ICommand ClearErrorCommand { get; }
        
        #endregion
        
        #region Events
        
        public event EventHandler<LoginSuccessEventArgs>? LoginSuccess;
        public event EventHandler<LoginFailedEventArgs>? LoginFailed;
        
        #endregion
        
        #region Methods
        
        private async Task LoginAsync()
        {
            try
            {
                IsLoggingIn = true;
                ClearError();
                StatusMessage = "جاري التحقق من بيانات المستخدم...";
                
                _logger.LogInformation("محاولة تسجيل دخول للمستخدم: {Username}", Username);
                
                // التحقق من بيانات المستخدم
                var user = await _dbService.AuthenticateUserAsync(Username, Password);
                
                if (user != null)
                {
                    // تسجيل نجح
                    await _auditService.LogAsync(
                        $"تم تسجيل دخول المستخدم {user.FullName}",
                        AuditActionType.Login,
                        user.UserId,
                        $"Login successful - Role: {user.Role}"
                    );
                    
                    StatusMessage = $"مرحباً {user.FullName}";
                    
                    // تحديث آخر تسجيل دخول
                    await _dbService.UpdateLastLoginAsync(user.UserId);
                    
                    _logger.LogInformation("نجح تسجيل الدخول للمستخدم {UserId} - {FullName}", user.UserId, user.FullName);
                    
                    // إطلاق حدث نجح تسجيل الدخول
                    LoginSuccess?.Invoke(this, new LoginSuccessEventArgs(user));
                }
                else
                {
                    // تسجيل فشل
                    await _auditService.LogAsync(
                        $"فشل تسجيل دخول للمستخدم {Username}",
                        AuditActionType.LoginFailed,
                        null,
                        $"Invalid credentials for user: {Username}"
                    );
                    
                    ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                    StatusMessage = "فشل في تسجيل الدخول";
                    
                    _logger.LogWarning("فشل في تسجيل الدخول للمستخدم: {Username}", Username);
                    
                    // إطلاق حدث فشل تسجيل الدخول
                    LoginFailed?.Invoke(this, new LoginFailedEventArgs("بيانات غير صحيحة"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل الدخول للمستخدم: {Username}", Username);
                
                ErrorMessage = "حدث خطأ في تسجيل الدخول. الرجاء المحاولة مرة أخرى.";
                StatusMessage = "خطأ في النظام";
                
                // تسجيل الخطأ في سجل المراجعة
                await _auditService.LogAsync(
                    $"خطأ في تسجيل الدخول: {ex.Message}",
                    AuditActionType.Error,
                    null,
                    $"Login error for user {Username}: {ex.Message}"
                );
                
                LoginFailed?.Invoke(this, new LoginFailedEventArgs($"خطأ في النظام: {ex.Message}"));
            }
            finally
            {
                IsLoggingIn = false;
            }
        }
        
        private void ClearError()
        {
            ErrorMessage = string.Empty;
        }
        
        private void UpdateCanLogin()
        {
            OnPropertyChanged(nameof(CanLogin));
        }
        
        public void ResetForm()
        {
            Username = string.Empty;
            Password = string.Empty;
            ClearError();
            StatusMessage = "أدخل بيانات المستخدم لتسجيل الدخول";
        }
        
        #endregion
    }
    
    public class LoginSuccessEventArgs : EventArgs
    {
        public User User { get; }
        
        public LoginSuccessEventArgs(User user)
        {
            User = user;
        }
    }
    
    public class LoginFailedEventArgs : EventArgs
    {
        public string Reason { get; }
        
        public LoginFailedEventArgs(string reason)
        {
            Reason = reason;
        }
    }
}