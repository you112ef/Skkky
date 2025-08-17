using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class PatientAttachment
    {
        [Key]
        public int AttachmentId { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty; // image, document, video
        
        [Required]
        [StringLength(100)]
        public string MimeType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        public AttachmentCategory Category { get; set; } = AttachmentCategory.Other;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public int? UploadedByUserId { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Thumbnail for images and videos
        public string? ThumbnailPath { get; set; }
        
        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;
        
        [ForeignKey("UploadedByUserId")]
        public virtual User? UploadedByUser { get; set; }
    }
    
    public class ExamAttachment
    {
        [Key]
        public int AttachmentId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = string.Empty; // image, video, document
        
        [Required]
        [StringLength(100)]
        public string MimeType { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [StringLength(200)]
        public string? Description { get; set; }
        
        public AttachmentCategory Category { get; set; } = AttachmentCategory.TestResult;
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public int? UploadedByUserId { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // For CASA video analysis
        public bool IsProcessed { get; set; } = false;
        
        public DateTime? ProcessedAt { get; set; }
        
        public string? ProcessingResults { get; set; } // JSON string with analysis results
        
        // Thumbnail for images and videos
        public string? ThumbnailPath { get; set; }
        
        // AI Analysis metadata
        public string? AiAnalysisData { get; set; } // JSON with YOLO/DeepSORT results
        
        public double? ConfidenceScore { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("UploadedByUserId")]
        public virtual User? UploadedByUser { get; set; }
    }
    
    public enum AttachmentCategory
    {
        ProfileImage = 1,      // صورة شخصية
        MedicalImage = 2,      // صورة طبية
        TestResult = 3,        // نتيجة فحص
        CasaVideo = 4,         // فيديو CASA
        MicroscopyImage = 5,   // صورة مجهر
        Document = 6,          // وثيقة
        Report = 7,            // تقرير
        Other = 8              // أخرى
    }
}