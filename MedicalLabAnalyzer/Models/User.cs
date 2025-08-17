using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required]
        public UserRole Role { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public int? CreatedByUserId { get; set; }
        
        public int? UpdatedByUserId { get; set; }
        
        public bool ForcePasswordChange { get; set; } = false;
        
        public string? ProfileImagePath { get; set; }
        
        // Navigation properties
        public virtual ICollection&lt;AuditLog&gt; AuditLogs { get; set; } = new List&lt;AuditLog&gt;();
        public virtual ICollection&lt;Exam&gt; ExamsCreated { get; set; } = new List&lt;Exam&gt;();
        public virtual ICollection&lt;Exam&gt; ExamsReviewed { get; set; } = new List&lt;Exam&gt;();
    }
    
    [Flags]
    public enum UserRole
    {
        All = 0,           // جميع الأدوار
        Manager = 1,       // مدير
        LabTechnician = 2, // فني مختبر  
        Receptionist = 4   // مستقبل
    }
}