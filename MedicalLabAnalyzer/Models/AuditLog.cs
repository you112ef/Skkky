using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string TableName { get; set; } = string.Empty;
        
        [Required]
        public int RecordId { get; set; }
        
        [Required]
        public AuditAction Action { get; set; }
        
        public string? OldValues { get; set; } // JSON format
        
        public string? NewValues { get; set; } // JSON format
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [Required]
        public int UserId { get; set; }
        
        [StringLength(45)]
        public string? IPAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Additional context for medical records
        public int? PatientId { get; set; }
        
        public int? ExamId { get; set; }
        
        [StringLength(100)]
        public string? ModuleName { get; set; } // CASA, CBC, Urine, etc.
        
        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
        
        // For tracking critical operations
        public bool IsCritical { get; set; } = false;
        
        [StringLength(200)]
        public string? CriticalReason { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
        
        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam? Exam { get; set; }
    }
    
    public enum AuditAction
    {
        Create = 1,     // إنشاء
        Read = 2,       // قراءة
        Update = 3,     // تحديث
        Delete = 4,     // حذف
        Login = 5,      // تسجيل دخول
        Logout = 6,     // تسجيل خروج
        Export = 7,     // تصدير
        Print = 8,      // طباعة
        Review = 9,     // مراجعة
        Approve = 10,   // موافقة
        Reject = 11,    // رفض
        Archive = 12,   // أرشفة
        Restore = 13,   // استرداد
        Backup = 14,    // نسخ احتياطي
        Import = 15,    // استيراد
        Calibrate = 16, // معايرة
        Analyze = 17,   // تحليل
        Upload = 18,    // رفع ملف
        Download = 19   // تحميل ملف
    }
    
    public enum AuditSeverity
    {
        Info = 1,       // معلومات
        Warning = 2,    // تحذير
        Error = 3,      // خطأ
        Critical = 4    // حرج
    }
}