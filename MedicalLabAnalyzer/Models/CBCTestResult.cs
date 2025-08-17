using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class CBCTestResult
    {
        [Key]
        public int CBCResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Red Blood Cell Parameters
        public double? RBC { get; set; } // 10^6/μL
        
        public double? Hemoglobin { get; set; } // g/dL
        
        public double? Hematocrit { get; set; } // %
        
        public double? MCV { get; set; } // fL (Mean Corpuscular Volume)
        
        public double? MCH { get; set; } // pg (Mean Corpuscular Hemoglobin)
        
        public double? MCHC { get; set; } // g/dL (Mean Corpuscular Hemoglobin Concentration)
        
        public double? RDW { get; set; } // % (Red Cell Distribution Width)
        
        // White Blood Cell Parameters
        public double? WBC { get; set; } // 10^3/μL
        
        public double? Neutrophils { get; set; } // %
        
        public double? Lymphocytes { get; set; } // %
        
        public double? Monocytes { get; set; } // %
        
        public double? Eosinophils { get; set; } // %
        
        public double? Basophils { get; set; } // %
        
        // Absolute Counts
        public double? NeutrophilsAbsolute { get; set; } // 10^3/μL
        
        public double? LymphocytesAbsolute { get; set; } // 10^3/μL
        
        public double? MonocytesAbsolute { get; set; } // 10^3/μL
        
        public double? EosinophilsAbsolute { get; set; } // 10^3/μL
        
        public double? BasophilsAbsolute { get; set; } // 10^3/μL
        
        // Platelet Parameters
        public double? Platelets { get; set; } // 10^3/μL
        
        public double? MPV { get; set; } // fL (Mean Platelet Volume)
        
        public double? PDW { get; set; } // fL (Platelet Distribution Width)
        
        public double? PCT { get; set; } // % (Plateletcrit)
        
        // Additional Parameters
        public double? ESR { get; set; } // mm/hr (Erythrocyte Sedimentation Rate)
        
        // Reticulocytes (if available)
        public double? Reticulocytes { get; set; } // %
        
        public double? ReticulocytesAbsolute { get; set; } // 10^3/μL
        
        // Morphology Notes
        [StringLength(500)]
        public string? RBCMorphology { get; set; }
        
        [StringLength(500)]
        public string? WBCMorphology { get; set; }
        
        [StringLength(500)]
        public string? PlateletMorphology { get; set; }
        
        // Analysis Information
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(100)]
        public string? AnalyzerUsed { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Interpretation
        public CBCInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Critical Values Flags
        public bool HasCriticalValues { get; set; } = false;
        
        [StringLength(500)]
        public string? CriticalValuesNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum CBCInterpretation
    {
        Normal = 1,              // طبيعي
        Anemia = 2,              // فقر دم
        Polycythemia = 3,        // زيادة كريات الدم الحمراء
        Leukopenia = 4,          // نقص كريات الدم البيضاء
        Leukocytosis = 5,        // زيادة كريات الدم البيضاء
        Thrombocytopenia = 6,    // نقص الصفائح الدموية
        Thrombocytosis = 7,      // زيادة الصفائح الدموية
        IronDeficiencyAnemia = 8, // فقر دم بسبب نقص الحديد
        MacrocyticAnemia = 9,    // فقر دم كبير الخلايا
        MicrocyticAnemia = 10,   // فقر دم صغير الخلايا
        InfectionSuspected = 11, // اشتباه عدوى
        AllergicReaction = 12    // رد فعل تحسسي
    }
}