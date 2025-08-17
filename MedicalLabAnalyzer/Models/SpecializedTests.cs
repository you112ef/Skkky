using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    // CRP Test Result
    public class CRPTestResult
    {
        [Key]
        public int CRPResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? CRP { get; set; } // mg/L
        
        public double? HighSensitivityCRP { get; set; } // mg/L
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public CRPInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum CRPInterpretation
    {
        Normal = 1,
        MildInflammation = 2,
        ModerateInflammation = 3,
        SevereInflammation = 4,
        CardiovascularRiskLow = 5,
        CardiovascularRiskModerate = 6,
        CardiovascularRiskHigh = 7
    }
    
    // Thyroid Test Result
    public class ThyroidTestResult
    {
        [Key]
        public int ThyroidResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? TSH { get; set; } // mIU/L
        
        public double? FreeT4 { get; set; } // ng/dL
        
        public double? FreeT3 { get; set; } // pg/mL
        
        public double? TotalT4 { get; set; } // μg/dL
        
        public double? TotalT3 { get; set; } // ng/dL
        
        public double? ReverseT3 { get; set; } // ng/dL
        
        public double? T3UptakeRatio { get; set; }
        
        public double? FreeThyroxineIndex { get; set; }
        
        // Antibodies
        public double? AntiTPOAntibodies { get; set; } // IU/mL
        
        public double? AntiThyroglobulinAntibodies { get; set; } // IU/mL
        
        public double? TSHReceptorAntibodies { get; set; } // IU/L
        
        // Additional
        public double? Thyroglobulin { get; set; } // ng/mL
        
        public double? Calcitonin { get; set; } // pg/mL
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public ThyroidInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum ThyroidInterpretation
    {
        Normal = 1,
        Hyperthyroidism = 2,
        Hypothyroidism = 3,
        SubclinicalHyperthyroidism = 4,
        SubclinicalHypothyroidism = 5,
        ThyroiditisRisk = 6,
        GravesDisease = 7,
        HashimotosThyroiditis = 8
    }
    
    // Electrolytes Test Result
    public class ElectrolytesTestResult
    {
        [Key]
        public int ElectrolytesResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? Sodium { get; set; } // mEq/L
        
        public double? Potassium { get; set; } // mEq/L
        
        public double? Chloride { get; set; } // mEq/L
        
        public double? CO2 { get; set; } // mEq/L
        
        public double? Calcium { get; set; } // mg/dL
        
        public double? IonicCalcium { get; set; } // mg/dL
        
        public double? Phosphorus { get; set; } // mg/dL
        
        public double? Magnesium { get; set; } // mg/dL
        
        // Calculated Parameters
        public double? AnionGap { get; set; }
        
        public double? OsmolarityCalculated { get; set; } // mOsm/kg
        
        public double? OsmolarityMeasured { get; set; } // mOsm/kg
        
        public double? OsmolalGap { get; set; }
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public ElectrolytesInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum ElectrolytesInterpretation
    {
        Normal = 1,
        Hyponatremia = 2,
        Hypernatremia = 3,
        Hypokalemia = 4,
        Hyperkalemia = 5,
        Hypochloremia = 6,
        Hyperchloremia = 7,
        MetabolicAcidosis = 8,
        MetabolicAlkalosis = 9,
        Hypocalcemia = 10,
        Hypercalcemia = 11,
        Hypomagnesemia = 12,
        Hypermagnesemia = 13
    }
    
    // Coagulation Test Result
    public class CoagulationTestResult
    {
        [Key]
        public int CoagulationResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        public double? ProthrombinTime { get; set; } // seconds
        
        public double? ProthrombinTimeControl { get; set; } // seconds
        
        public double? INR { get; set; }
        
        public double? ActivatedPartialThromboplastinTime { get; set; } // seconds (aPTT)
        
        public double? aPTTControl { get; set; } // seconds
        
        public double? ThrombromeTime { get; set; } // seconds
        
        public double? FibrinogenLevel { get; set; } // mg/dL
        
        public double? DDimer { get; set; } // ng/mL
        
        public double? PlasminogenLevel { get; set; } // %
        
        public double? AntithrombinIII { get; set; } // %
        
        public double? ProteinC { get; set; } // %
        
        public double? ProteinS { get; set; } // %
        
        public double? FactorVLeiden { get; set; }
        
        public double? ProthrombinG20210A { get; set; }
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public CoagulationInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum CoagulationInterpretation
    {
        Normal = 1,
        HypercoagulableState = 2,
        HypocoagulableState = 3,
        ThrombosisRisk = 4,
        BleedingDisorder = 5,
        AnticoagulantMonitoring = 6,
        DICRisk = 7
    }
}