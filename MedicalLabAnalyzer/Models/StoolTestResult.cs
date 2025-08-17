using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    // Stool Test Result
    public class StoolTestResult
    {
        [Key]
        public int StoolResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Physical Properties
        [StringLength(50)]
        public string? Color { get; set; }
        
        [StringLength(50)]
        public string? Consistency { get; set; }
        
        [StringLength(50)]
        public string? Odor { get; set; }
        
        // Chemical Tests
        public string? OccultBlood { get; set; } // Negative, Positive
        
        public string? ReducingSubstances { get; set; } // Negative, Positive
        
        public double? pH { get; set; }
        
        // Microscopic Examination
        public string? RBCs { get; set; }
        
        public string? WBCs { get; set; }
        
        public string? EpithelialCells { get; set; }
        
        public string? Bacteria { get; set; }
        
        public string? Yeast { get; set; }
        
        public string? Parasites { get; set; }
        
        public string? ParasiteOva { get; set; }
        
        public string? Cysts { get; set; }
        
        public string? Muscle fibers { get; set; }
        
        public string? StarchGranules { get; set; }
        
        public string? Fat { get; set; }
        
        // Additional Tests
        public double? Calprotectin { get; set; } // μg/g
        
        public double? Lactoferrin { get; set; } // μg/g
        
        public string? ClostriDifficileToxin { get; set; }
        
        public string? HelicobacterPyloriAntigen { get; set; }
        
        // Analysis Information
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Interpretation
        public StoolInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum StoolInterpretation
    {
        Normal = 1,
        ParasiteInfection = 2,
        BacterialInfection = 3,
        IBDSuspected = 4,
        Malabsorption = 5,
        GIBleeding = 6
    }
    
    // Glucose Test Result
    public class GlucoseTestResult
    {
        [Key]
        public int GlucoseResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? FastingGlucose { get; set; } // mg/dL
        
        public double? RandomGlucose { get; set; } // mg/dL
        
        public double? PostPrandialGlucose { get; set; } // mg/dL (2 hours after meal)
        
        public double? HbA1c { get; set; } // %
        
        // OGTT (Oral Glucose Tolerance Test)
        public double? OGTTBaseline { get; set; } // mg/dL
        
        public double? OGTT30min { get; set; } // mg/dL
        
        public double? OGTT60min { get; set; } // mg/dL
        
        public double? OGTT90min { get; set; } // mg/dL
        
        public double? OGTT120min { get; set; } // mg/dL
        
        // Additional Parameters
        public double? Fructosamine { get; set; } // μmol/L
        
        public double? C_Peptide { get; set; } // ng/mL
        
        public double? Insulin { get; set; } // μU/mL
        
        // Analysis Information
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        // Test Conditions
        public bool IsFasting { get; set; } = false;
        
        public int? FastingHours { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Interpretation
        public GlucoseInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum GlucoseInterpretation
    {
        Normal = 1,
        PreDiabetes = 2,
        DiabetesMellitus = 3,
        Hypoglycemia = 4,
        ImpairedGlucoseTolerance = 5,
        ImpairedFastingGlucose = 6
    }
}