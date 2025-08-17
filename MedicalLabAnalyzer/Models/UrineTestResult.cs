using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    public class UrineTestResult
    {
        [Key]
        public int UrineResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Physical Properties
        [StringLength(50)]
        public string? Color { get; set; } = "Yellow";
        
        [StringLength(50)]
        public string? Appearance { get; set; } = "Clear";
        
        public double? SpecificGravity { get; set; }
        
        public double? pH { get; set; }
        
        // Chemical Analysis
        public string? Protein { get; set; } // Negative, Trace, +1, +2, +3, +4
        
        public string? Glucose { get; set; } // Negative, Trace, +1, +2, +3, +4
        
        public string? Ketones { get; set; } // Negative, Trace, Small, Moderate, High
        
        public string? Blood { get; set; } // Negative, Trace, +1, +2, +3
        
        public string? Bilirubin { get; set; } // Negative, +1, +2, +3
        
        public string? Urobilinogen { get; set; } // Normal, +1, +2, +3, +4
        
        public string? Nitrites { get; set; } // Negative, Positive
        
        public string? LeukocyteEsterase { get; set; } // Negative, Trace, +1, +2, +3
        
        // Microscopic Examination
        public string? RBCsMicroscopy { get; set; } // per hpf
        
        public string? WBCsMicroscopy { get; set; } // per hpf
        
        public string? EpithelialCells { get; set; } // Few, Moderate, Many
        
        public string? Bacteria { get; set; } // None, Few, Moderate, Many
        
        public string? Casts { get; set; } // Type and number
        
        public string? Crystals { get; set; } // Type and amount
        
        public string? Mucus { get; set; } // None, Few, Moderate, Many
        
        public string? Yeast { get; set; } // None, Few, Moderate, Many
        
        public string? Parasites { get; set; }
        
        // Additional Tests
        public double? Microalbumin { get; set; } // mg/L
        
        public double? CreatinineClearance { get; set; } // mL/min
        
        public double? UrineCreatinine { get; set; } // mg/dL
        
        public double? UrineSodium { get; set; } // mEq/L
        
        public double? UrinePotassium { get; set; } // mEq/L
        
        // Analysis Information
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Collection Information
        [StringLength(100)]
        public string? CollectionMethod { get; set; } // Clean catch, Catheter, etc.
        
        public DateTime? CollectionTime { get; set; }
        
        public bool IsFirstMorningSpecimen { get; set; } = false;
        
        // Interpretation
        public UrineInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum UrineInterpretation
    {
        Normal = 1,                    // طبيعي
        UTI = 2,                      // التهاب المسالك البولية
        Proteinuria = 3,              // البروتين في البول
        Hematuria = 4,                // الدم في البول
        Glucosuria = 5,               // السكر في البول
        Ketonuria = 6,                // الكيتونات في البول
        KidneyDiseaseSuspected = 7,   // اشتباه مرض كلوي
        DiabetesSuspected = 8,        // اشتباه سكري
        LiverDiseaseSuspected = 9,    // اشتباه مرض كبد
        ConcentrationDefect = 10      // خلل في التركيز
    }
}