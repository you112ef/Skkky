using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExamNumber { get; set; } = string.Empty;
        
        [Required]
        public ExamType ExamType { get; set; }
        
        [Required]
        public DateTime ExamDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? CollectionDate { get; set; }
        
        public DateTime? CompletedDate { get; set; }
        
        public DateTime? ReviewedDate { get; set; }
        
        [Required]
        public ExamStatus Status { get; set; } = ExamStatus.Pending;
        
        public int? CreatedByUserId { get; set; }
        
        public int? ReviewedByUserId { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [StringLength(500)]
        public string? DoctorNotes { get; set; }
        
        [StringLength(100)]
        public string? ReferringPhysician { get; set; }
        
        public bool IsUrgent { get; set; } = false;
        
        public bool IsFasting { get; set; } = false;
        
        [StringLength(200)]
        public string? SpecimenType { get; set; }
        
        [StringLength(200)]
        public string? SpecimenContainer { get; set; }
        
        public string? ClinicalHistory { get; set; }
        
        public decimal? TotalCost { get; set; }
        
        public decimal? PaidAmount { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;
        
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }
        
        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedByUser { get; set; }
        
        // Test Results - One-to-one relationships
        public virtual CASAResult? CASAResult { get; set; }
        public virtual CBCTestResult? CBCResult { get; set; }
        public virtual UrineTestResult? UrineResult { get; set; }
        public virtual StoolTestResult? StoolResult { get; set; }
        public virtual GlucoseTestResult? GlucoseResult { get; set; }
        public virtual LipidProfileTestResult? LipidProfileResult { get; set; }
        public virtual LiverFunctionTestResult? LiverFunctionResult { get; set; }
        public virtual KidneyFunctionTestResult? KidneyFunctionResult { get; set; }
        public virtual CRPTestResult? CRPResult { get; set; }
        public virtual ThyroidTestResult? ThyroidResult { get; set; }
        public virtual ElectrolytesTestResult? ElectrolytesResult { get; set; }
        public virtual CoagulationTestResult? CoagulationResult { get; set; }
        public virtual VitaminTestResult? VitaminResult { get; set; }
        public virtual HormoneTestResult? HormoneResult { get; set; }
        public virtual MicrobiologyTestResult? MicrobiologyResult { get; set; }
        public virtual PCRTestResult? PCRResult { get; set; }
        public virtual SerologyTestResult? SerologyResult { get; set; }
        
        // Attachments
        public virtual ICollection&lt;ExamAttachment&gt; Attachments { get; set; } = new List&lt;ExamAttachment&gt;();
        public virtual ICollection&lt;AuditLog&gt; AuditLogs { get; set; } = new List&lt;AuditLog&gt;();
    }
    
    public enum ExamType
    {
        CASA = 1,           // تحليل الحيوانات المنوية
        CBC = 2,            // تعداد الدم الكامل  
        Urine = 3,          // تحليل البول
        Stool = 4,          // تحليل البراز
        Glucose = 5,        // السكر
        LipidProfile = 6,   // دهون الدم
        LiverFunction = 7,  // وظائف الكبد
        KidneyFunction = 8, // وظائف الكلى
        CRP = 9,           // البروتين التفاعلي
        Thyroid = 10,      // الغدة الدرقية
        Electrolytes = 11, // الأملاح
        Coagulation = 12,  // التجلط
        Vitamin = 13,      // الفيتامينات
        Hormones = 14,     // الهرمونات
        Microbiology = 15, // الميكروبيولوجي
        PCR = 16,          // تفاعل البوليمراز المتسلسل
        Serology = 17      // الأمصال
    }
    
    public enum ExamStatus
    {
        Pending = 1,       // في الانتظار
        InProgress = 2,    // قيد التنفيذ
        Completed = 3,     // مكتمل
        Reviewed = 4,      // تمت المراجعة
        Rejected = 5,      // مرفوض
        Cancelled = 6      // ملغي
    }
}