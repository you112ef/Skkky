using System.ComponentModel.DataAnnotations;

namespace MedicalLabAnalyzer.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        public DateTime DateOfBirth { get; set; }
        
        [Required]
        public Gender Gender { get; set; }
        
        [StringLength(20)]
        public string? NationalId { get; set; }
        
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(200)]
        public string? Address { get; set; }
        
        [StringLength(50)]
        public string? EmergencyContact { get; set; }
        
        [StringLength(20)]
        public string? EmergencyPhone { get; set; }
        
        public string? MedicalHistory { get; set; }
        
        public string? Allergies { get; set; }
        
        public string? CurrentMedications { get; set; }
        
        public string? ProfileImagePath { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [StringLength(50)]
        public string? BloodGroup { get; set; }
        
        public double? Height { get; set; } // cm
        
        public double? Weight { get; set; } // kg
        
        // Calculated property
        public double? BMI
        {
            get
            {
                if (Height.HasValue && Weight.HasValue && Height.Value > 0)
                {
                    return Math.Round(Weight.Value / Math.Pow(Height.Value / 100, 2), 2);
                }
                return null;
            }
        }
        
        public int Age
        {
            get
            {
                return DateTime.Today.Year - DateOfBirth.Year -
                    (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
            }
        }
        
        // Navigation properties
        public virtual ICollection&lt;Exam&gt; Exams { get; set; } = new List&lt;Exam&gt;();
        public virtual ICollection&lt;PatientAttachment&gt; Attachments { get; set; } = new List&lt;PatientAttachment&gt;();
    }
    
    public enum Gender
    {
        Male = 1,    // ذكر
        Female = 2   // أنثى
    }
}