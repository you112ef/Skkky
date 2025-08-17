using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    // Lipid Profile Test Result
    public class LipidProfileTestResult
    {
        [Key]
        public int LipidProfileResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? TotalCholesterol { get; set; } // mg/dL
        
        public double? Triglycerides { get; set; } // mg/dL
        
        public double? HDLCholesterol { get; set; } // mg/dL
        
        public double? LDLCholesterol { get; set; } // mg/dL
        
        public double? VLDLCholesterol { get; set; } // mg/dL
        
        public double? NonHDLCholesterol { get; set; } // mg/dL
        
        public double? CholesterolHDLRatio { get; set; }
        
        public double? LDLHDLRatio { get; set; }
        
        public double? TriglyceridesHDLRatio { get; set; }
        
        // Additional Lipid Parameters
        public double? ApolipoproteinA1 { get; set; } // mg/dL
        
        public double? ApolipoproteinB { get; set; } // mg/dL
        
        public double? ApoB_ApoA1_Ratio { get; set; }
        
        public double? Lipoprotein_a { get; set; } // mg/dL
        
        // Analysis Information
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        // Test Conditions
        public bool IsFasting { get; set; } = true;
        
        public int? FastingHours { get; set; }
        
        // Quality Control
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        // Interpretation
        public LipidInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum LipidInterpretation
    {
        Normal = 1,
        Hypercholesterolemia = 2,
        Hypertriglyceridemia = 3,
        MixedDyslipidemia = 4,
        LowHDL = 5,
        HighLDL = 6,
        CardiovascularRiskHigh = 7
    }
    
    // Liver Function Test Result
    public class LiverFunctionTestResult
    {
        [Key]
        public int LiverFunctionResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Liver Enzymes
        public double? ALT { get; set; } // U/L (Alanine Aminotransferase)
        
        public double? AST { get; set; } // U/L (Aspartate Aminotransferase)
        
        public double? ALP { get; set; } // U/L (Alkaline Phosphatase)
        
        public double? GGT { get; set; } // U/L (Gamma Glutamyl Transferase)
        
        public double? LDH { get; set; } // U/L (Lactate Dehydrogenase)
        
        // Bilirubin
        public double? TotalBilirubin { get; set; } // mg/dL
        
        public double? DirectBilirubin { get; set; } // mg/dL
        
        public double? IndirectBilirubin { get; set; } // mg/dL
        
        // Proteins
        public double? TotalProtein { get; set; } // g/dL
        
        public double? Albumin { get; set; } // g/dL
        
        public double? Globulin { get; set; } // g/dL
        
        public double? AlbuminGlobulinRatio { get; set; }
        
        // Coagulation
        public double? ProthrombinTime { get; set; } // seconds
        
        public double? INR { get; set; }
        
        // Additional Tests
        public double? AmmoniaNH3 { get; set; } // μg/dL
        
        public double? AlphaFetoprotein { get; set; } // ng/mL
        
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
        public LiverInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum LiverInterpretation
    {
        Normal = 1,
        Hepatocellular = 2,
        Cholestatic = 3,
        Mixed = 4,
        CirrhosisRisk = 5,
        AcuteHepatitis = 6,
        ChronicHepatitis = 7
    }
    
    // Kidney Function Test Result
    public class KidneyFunctionTestResult
    {
        [Key]
        public int KidneyFunctionResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Basic Kidney Function
        public double? Creatinine { get; set; } // mg/dL
        
        public double? BUN { get; set; } // mg/dL (Blood Urea Nitrogen)
        
        public double? UricAcid { get; set; } // mg/dL
        
        public double? BUN_CreatinineRatio { get; set; }
        
        // Calculated Values
        public double? eGFR { get; set; } // mL/min/1.73m² (estimated Glomerular Filtration Rate)
        
        public double? CreatinineClearance { get; set; } // mL/min
        
        // Electrolytes
        public double? Sodium { get; set; } // mEq/L
        
        public double? Potassium { get; set; } // mEq/L
        
        public double? Chloride { get; set; } // mEq/L
        
        public double? CO2 { get; set; } // mEq/L
        
        // Additional Tests
        public double? Phosphorus { get; set; } // mg/dL
        
        public double? Calcium { get; set; } // mg/dL
        
        public double? Magnesium { get; set; } // mg/dL
        
        public double? Microalbumin { get; set; } // mg/L
        
        public double? AlbuminCreatinineRatio { get; set; } // mg/g
        
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
        public KidneyInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        // Navigation properties
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum KidneyInterpretation
    {
        Normal = 1,
        CKDStage1 = 2,
        CKDStage2 = 3,
        CKDStage3a = 4,
        CKDStage3b = 5,
        CKDStage4 = 6,
        CKDStage5 = 7,
        AcuteKidneyInjury = 8,
        ElectrolyteImbalance = 9
    }
}