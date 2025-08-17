using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class CASAResult
    {
        [Key]
        public int CASAResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Basic Analysis Data
        public int? TotalSpermCount { get; set; }
        
        public double? Concentration { get; set; } // مليون/مل
        
        public double? Volume { get; set; } // مل
        
        public double? TotalMotileSperm { get; set; } // مليون
        
        // Motility Analysis
        public double? ProgressiveMotility { get; set; } // PR %
        
        public double? NonProgressiveMotility { get; set; } // NP %
        
        public double? ImmotileSperm { get; set; } // IM %
        
        public double? TotalMotility { get; set; } // PR + NP %
        
        // Velocity Parameters (μm/s)
        public double? VCL { get; set; } // Curvilinear Velocity
        
        public double? VSL { get; set; } // Straight Line Velocity
        
        public double? VAP { get; set; } // Average Path Velocity
        
        // Derived Parameters
        public double? LIN { get; set; } // Linearity = VSL/VCL
        
        public double? STR { get; set; } // Straightness = VSL/VAP
        
        public double? WOB { get; set; } // Wobble = VAP/VCL
        
        public double? ALH { get; set; } // Amplitude of Lateral Head displacement
        
        public double? BCF { get; set; } // Beat Cross Frequency
        
        // Morphology Analysis
        public double? NormalMorphology { get; set; } // %
        
        public double? HeadDefects { get; set; } // %
        
        public double? MidpieceDefects { get; set; } // %
        
        public double? TailDefects { get; set; } // %
        
        // Viability
        public double? Viability { get; set; } // %
        
        // Analysis Settings
        public double? TemperatureC { get; set; } = 37.0;
        
        public double? pHValue { get; set; }
        
        public int? AnalysisDurationSeconds { get; set; }
        
        public int? FramesAnalyzed { get; set; }
        
        public double? FrameRate { get; set; } // FPS
        
        // AI Analysis Results
        public int? DetectedSpermCount { get; set; } // من الذكاء الاصطناعي
        
        public int? TrackedSpermCount { get; set; } // المتتبعة بـ DeepSORT
        
        public double? AiConfidenceAverage { get; set; }
        
        public string? YoloDetectionData { get; set; } // JSON with YOLO results
        
        public string? DeepSortTrackingData { get; set; } // JSON with DeepSORT results
        
        // Video/Image Analysis
        public string? VideoFilePath { get; set; }
        
        public string? ProcessedVideoPath { get; set; }
        
        public string? AnalysisImagePaths { get; set; } // JSON array of image paths
        
        public bool IsAiAnalysisCompleted { get; set; } = false;
        
        public DateTime? AiAnalysisCompletedAt { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Calibration Data
        public double? MicronPerPixel { get; set; } // المعايرة المجهرية
        
        public double? MagnificationUsed { get; set; }
        
        // Analysis Metadata
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        [StringLength(500)]
        public string? AnalysisNotes { get; set; }
        
        // Device/Equipment Information
        [StringLength(100)]
        public string? EquipmentUsed { get; set; }
        
        [StringLength(100)]
        public string? SoftwareVersion { get; set; } = "1.0.0";
        
        // Interpretation
        public CASAInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum CASAInterpretation
    {
        Normal = 1,           // طبيعي
        Oligospermia = 2,     // قلة الحيوانات المنوية
        Asthenospermia = 3,   // ضعف حركة الحيوانات المنوية  
        Teratospermia = 4,    // تشوه الحيوانات المنوية
        Azoospermia = 5,      // عدم وجود حيوانات منوية
        OligoAsthenospermia = 6,        // قلة وضعف حركة
        OligoTeratospermia = 7,         // قلة وتشوه
        AsthenoTeratospermia = 8,       // ضعف حركة وتشوه
        OligoAsthenoTeratospermia = 9   // قلة وضعف حركة وتشوه
    }
}