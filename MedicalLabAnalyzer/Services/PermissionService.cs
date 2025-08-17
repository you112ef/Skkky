using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.Services
{
    public class PermissionService
    {
        private readonly ILogger<PermissionService> _logger;
        private readonly AuthenticationService _authService;
        private readonly AuditService _auditService;
        
        // خريطة الصلاحيات لكل دور
        private readonly Dictionary<UserRole, HashSet<Permission>> _rolePermissions;
        
        public PermissionService(
            ILogger<PermissionService> logger,
            AuthenticationService authService,
            AuditService auditService)
        {
            _logger = logger;
            _authService = authService;
            _auditService = auditService;
            
            _rolePermissions = InitializeRolePermissions();
        }
        
        #region Public Methods
        
        /// <summary>
        /// التحقق من صلاحية محددة
        /// </summary>
        public bool HasPermission(Permission permission)
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null || !currentUser.IsActive)
                {
                    LogPermissionCheck(permission, false, "لا يوجد مستخدم مسجل");
                    return false;
                }
                
                // المدير يملك كل الصلاحيات
                if (currentUser.Role == UserRole.Manager)
                {
                    LogPermissionCheck(permission, true, "مدير النظام");
                    return true;
                }
                
                // التحقق من الصلاحية
                var hasPermission = _rolePermissions.ContainsKey(currentUser.Role) &&
                                   _rolePermissions[currentUser.Role].Contains(permission);
                
                LogPermissionCheck(permission, hasPermission, $"دور المستخدم: {currentUser.Role}");
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من الصلاحية {Permission}", permission);
                return false;
            }
        }
        
        /// <summary>
        /// التحقق من إحدى الصلاحيات المتعددة
        /// </summary>
        public bool HasAnyPermission(params Permission[] permissions)
        {
            return permissions.Any(HasPermission);
        }
        
        /// <summary>
        /// التحقق من جميع الصلاحيات المطلوبة
        /// </summary>
        public bool HasAllPermissions(params Permission[] permissions)
        {
            return permissions.All(HasPermission);
        }
        
        /// <summary>
        /// الحصول على قائمة الصلاحيات للدور
        /// </summary>
        public HashSet<Permission> GetRolePermissions(UserRole role)
        {
            if (role == UserRole.Manager)
            {
                // المدير يملك كل الصلاحيات
                return new HashSet<Permission>(Enum.GetValues<Permission>());
            }
            
            return _rolePermissions.ContainsKey(role) 
                ? new HashSet<Permission>(_rolePermissions[role]) 
                : new HashSet<Permission>();
        }
        
        /// <summary>
        /// التحقق من إمكانية الوصول لصفحة معينة
        /// </summary>
        public bool CanAccessPage(string pageName)
        {
            var permission = GetPagePermission(pageName);
            return permission == null || HasPermission(permission.Value);
        }
        
        /// <summary>
        /// التحقق من إمكانية تنفيذ عملية معينة
        /// </summary>
        public bool CanPerformAction(string actionName)
        {
            var permission = GetActionPermission(actionName);
            return permission == null || HasPermission(permission.Value);
        }
        
        /// <summary>
        /// تسجيل عملية رفض الصلاحية
        /// </summary>
        public async Task LogPermissionDeniedAsync(Permission permission, string context = "")
        {
            var currentUser = _authService.CurrentUser;
            var message = $"تم رفض الصلاحية {permission} للمستخدم {currentUser?.FullName ?? "غير محدد"}";
            
            if (!string.IsNullOrEmpty(context))
                message += $" - السياق: {context}";
            
            await _auditService.LogAsync(
                message,
                AuditActionType.PermissionDenied,
                currentUser?.UserId,
                $"Permission denied: {permission} - Context: {context}"
            );
            
            _logger.LogWarning("تم رفض الصلاحية {Permission} للمستخدم {UserId} - السياق: {Context}", 
                permission, currentUser?.UserId, context);
        }
        
        #endregion
        
        #region Private Methods
        
        private Dictionary<UserRole, HashSet<Permission>> InitializeRolePermissions()
        {
            return new Dictionary<UserRole, HashSet<Permission>>
            {
                // صلاحيات المدير - كل الصلاحيات (يتم التعامل معها بشكل خاص في الكود)
                [UserRole.Manager] = new HashSet<Permission>(Enum.GetValues<Permission>()),
                
                // صلاحيات فني المختبر
                [UserRole.LabTechnician] = new HashSet<Permission>
                {
                    // إدارة المرضى
                    Permission.ViewPatients,
                    Permission.AddPatient,
                    Permission.EditPatient,
                    
                    // إدارة الفحوصات
                    Permission.ViewExams,
                    Permission.AddExam,
                    Permission.EditExam,
                    Permission.AnalyzeExam,
                    Permission.ViewExamResults,
                    
                    // تحليل CASA
                    Permission.CASAAnalysis,
                    Permission.CASACalibration,
                    Permission.ViewCASAResults,
                    
                    // تحليل الصور
                    Permission.ImageAnalysis,
                    Permission.ViewImageResults,
                    Permission.UploadImages,
                    Permission.UploadVideos,
                    
                    // التقارير
                    Permission.ViewReports,
                    Permission.GenerateReports,
                    Permission.PrintReports,
                    
                    // الأساسيات
                    Permission.ViewDashboard,
                    Permission.ChangeOwnPassword
                },
                
                // صلاحيات المستقبل
                [UserRole.Receptionist] = new HashSet<Permission>
                {
                    // إدارة المرضى (قراءة وإضافة فقط)
                    Permission.ViewPatients,
                    Permission.AddPatient,
                    
                    // إدارة الفحوصات (قراءة وإضافة فقط)
                    Permission.ViewExams,
                    Permission.AddExam,
                    Permission.ViewExamResults,
                    
                    // التقارير (قراءة وطباعة فقط)
                    Permission.ViewReports,
                    Permission.PrintReports,
                    
                    // الأساسيات
                    Permission.ViewDashboard,
                    Permission.ChangeOwnPassword
                }
            };
        }
        
        private Permission? GetPagePermission(string pageName)
        {
            var pagePermissions = new Dictionary<string, Permission>
            {
                ["Dashboard"] = Permission.ViewDashboard,
                ["Patients"] = Permission.ViewPatients,
                ["Exams"] = Permission.ViewExams,
                ["CASA"] = Permission.CASAAnalysis,
                ["Reports"] = Permission.ViewReports,
                ["Calibration"] = Permission.CASACalibration,
                ["Users"] = Permission.ManageUsers,
                ["Audit"] = Permission.ViewAuditLog,
                ["Settings"] = Permission.ManageSettings
            };
            
            return pagePermissions.ContainsKey(pageName) ? pagePermissions[pageName] : null;
        }
        
        private Permission? GetActionPermission(string actionName)
        {
            var actionPermissions = new Dictionary<string, Permission>
            {
                ["AddPatient"] = Permission.AddPatient,
                ["EditPatient"] = Permission.EditPatient,
                ["DeletePatient"] = Permission.DeletePatient,
                ["AddExam"] = Permission.AddExam,
                ["EditExam"] = Permission.EditExam,
                ["DeleteExam"] = Permission.DeleteExam,
                ["AnalyzeExam"] = Permission.AnalyzeExam,
                ["UploadImage"] = Permission.UploadImages,
                ["UploadVideo"] = Permission.UploadVideos,
                ["GenerateReport"] = Permission.GenerateReports,
                ["PrintReport"] = Permission.PrintReports,
                ["ManageUser"] = Permission.ManageUsers,
                ["ViewAudit"] = Permission.ViewAuditLog,
                ["CASACalibration"] = Permission.CASACalibration
            };
            
            return actionPermissions.ContainsKey(actionName) ? actionPermissions[actionName] : null;
        }
        
        private void LogPermissionCheck(Permission permission, bool granted, string reason)
        {
            var currentUser = _authService.CurrentUser;
            _logger.LogDebug("فحص الصلاحية {Permission} للمستخدم {UserId}: {Result} - السبب: {Reason}",
                permission, currentUser?.UserId, granted ? "مسموح" : "مرفوض", reason);
        }
        
        #endregion
    }
    
    /// <summary>
    /// تعداد الصلاحيات في النظام
    /// </summary>
    public enum Permission
    {
        // صلاحيات لوحة التحكم
        ViewDashboard,
        
        // صلاحيات المرضى
        ViewPatients,
        AddPatient,
        EditPatient,
        DeletePatient,
        ViewPatientHistory,
        
        // صلاحيات الفحوصات
        ViewExams,
        AddExam,
        EditExam,
        DeleteExam,
        AnalyzeExam,
        ViewExamResults,
        EditExamResults,
        
        // صلاحيات CASA
        CASAAnalysis,
        CASACalibration,
        ViewCASAResults,
        EditCASAResults,
        
        // صلاحيات الصور والفيديو
        ImageAnalysis,
        ViewImageResults,
        UploadImages,
        UploadVideos,
        DeleteMedia,
        
        // صلاحيات التقارير
        ViewReports,
        GenerateReports,
        PrintReports,
        ExportReports,
        CustomReports,
        
        // صلاحيات إدارة المستخدمين
        ViewUsers,
        ManageUsers,
        ResetPasswords,
        
        // صلاحيات النظام
        ViewAuditLog,
        ManageSettings,
        BackupDatabase,
        RestoreDatabase,
        ViewSystemInfo,
        
        // صلاحيات شخصية
        ChangeOwnPassword,
        ViewOwnProfile,
        EditOwnProfile
    }
}