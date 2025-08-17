using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalLabAnalyzer.Models
{
    // Vitamin Test Result
    public class VitaminTestResult
    {
        [Key]
        public int VitaminResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Fat-soluble Vitamins
        public double? VitaminA { get; set; } // μg/dL
        
        public double? VitaminD25OH { get; set; } // ng/mL
        
        public double? VitaminD125OH { get; set; } // pg/mL
        
        public double? VitaminE { get; set; } // mg/dL
        
        public double? VitaminK { get; set; } // ng/mL
        
        // Water-soluble Vitamins
        public double? VitaminB1Thiamine { get; set; } // ng/mL
        
        public double? VitaminB2Riboflavin { get; set; } // ng/mL
        
        public double? VitaminB3Niacin { get; set; } // μg/mL
        
        public double? VitaminB5PantothenicAcid { get; set; } // μg/mL
        
        public double? VitaminB6Pyridoxine { get; set; } // ng/mL
        
        public double? VitaminB7Biotin { get; set; } // ng/mL
        
        public double? VitaminB9FolicAcid { get; set; } // ng/mL
        
        public double? VitaminB12Cobalamin { get; set; } // pg/mL
        
        public double? VitaminC { get; set; } // mg/dL
        
        // Related Parameters
        public double? Homocysteine { get; set; } // μmol/L
        
        public double? MethylmalonicAcid { get; set; } // nmol/L
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public VitaminInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum VitaminInterpretation
    {
        Normal = 1,
        VitaminDDeficiency = 2,
        VitaminB12Deficiency = 3,
        FolateDeficiency = 4,
        MultipleDeficiencies = 5,
        VitaminDInsufficiency = 6,
        VitaminToxicity = 7
    }
    
    // Hormone Test Result
    public class HormoneTestResult
    {
        [Key]
        public int HormoneResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Reproductive Hormones
        public double? FSH { get; set; } // mIU/mL (Follicle Stimulating Hormone)
        
        public double? LH { get; set; } // mIU/mL (Luteinizing Hormone)
        
        public double? Estradiol { get; set; } // pg/mL
        
        public double? Progesterone { get; set; } // ng/mL
        
        public double? Testosterone { get; set; } // ng/dL
        
        public double? FreeTestosterone { get; set; } // pg/mL
        
        public double? DHEAS { get; set; } // μg/dL (Dehydroepiandrosterone sulfate)
        
        public double? SHBG { get; set; } // nmol/L (Sex Hormone Binding Globulin)
        
        public double? Prolactin { get; set; } // ng/mL
        
        public double? hCG { get; set; } // mIU/mL
        
        public double? AMH { get; set; } // ng/mL (Anti-Müllerian Hormone)
        
        // Stress Hormones
        public double? Cortisol { get; set; } // μg/dL
        
        public double? CortisolFree { get; set; } // μg/24h
        
        public double? ACTH { get; set; } // pg/mL
        
        public double? Aldosterone { get; set; } // ng/dL
        
        // Growth Hormones
        public double? GrowthHormone { get; set; } // ng/mL
        
        public double? IGF1 { get; set; } // ng/mL (Insulin-like Growth Factor 1)
        
        // Pancreatic Hormones
        public double? Insulin { get; set; } // μU/mL
        
        public double? CPeptide { get; set; } // ng/mL
        
        public double? Glucagon { get; set; } // pg/mL
        
        // Other Hormones
        public double? Parathormone { get; set; } // pg/mL (PTH)
        
        public double? Calcitonin { get; set; } // pg/mL
        
        public double? Vasopressin { get; set; } // pg/mL (ADH)
        
        public double? Leptin { get; set; } // ng/mL
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public HormoneInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum HormoneInterpretation
    {
        Normal = 1,
        Hypogonadism = 2,
        PCOS = 3,
        Menopause = 4,
        Hyperthyroidism = 5,
        Hypothyroidism = 6,
        AdrenalInsufficiency = 7,
        CushingSyndrome = 8,
        DiabetesRisk = 9,
        InfertilityRelated = 10
    }
    
    // Microbiology Test Result
    public class MicrobiologyTestResult
    {
        [Key]
        public int MicrobiologyResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        [StringLength(100)]
        public string? SpecimenType { get; set; } // Blood, Urine, Stool, CSF, etc.
        
        [StringLength(100)]
        public string? CollectionSite { get; set; }
        
        [StringLength(200)]
        public string? GramStainResult { get; set; }
        
        [StringLength(200)]
        public string? DirectExamination { get; set; }
        
        // Culture Results
        [StringLength(200)]
        public string? PrimaryIsolate { get; set; }
        
        [StringLength(200)]
        public string? SecondaryIsolate { get; set; }
        
        [StringLength(200)]
        public string? TertiaryIsolate { get; set; }
        
        [StringLength(200)]
        public string? ColonyCount { get; set; }
        
        // Organism Identification
        [StringLength(200)]
        public string? OrganismIdentified { get; set; }
        
        [StringLength(100)]
        public string? IdentificationMethod { get; set; }
        
        // Antimicrobial Susceptibility Testing
        public string? AntibioticSusceptibility { get; set; } // JSON format
        
        [StringLength(200)]
        public string? ResistancePattern { get; set; }
        
        [StringLength(200)]
        public string? ESBLProduction { get; set; }
        
        [StringLength(200)]
        public string? MRSAStatus { get; set; }
        
        [StringLength(200)]
        public string? VREStatus { get; set; }
        
        // Additional Tests
        [StringLength(200)]
        public string? CatalaseTest { get; set; }
        
        [StringLength(200)]
        public string? OxidaseTest { get; set; }
        
        [StringLength(200)]
        public string? CoagulaseTest { get; set; }
        
        [StringLength(200)]
        public string? IndoleTest { get; set; }
        
        // Fungal Studies
        [StringLength(200)]
        public string? KOHPreparation { get; set; }
        
        [StringLength(200)]
        public string? FungalCulture { get; set; }
        
        [StringLength(200)]
        public string? YeastIdentification { get; set; }
        
        // Parasitology
        [StringLength(200)]
        public string? OvaAndParasites { get; set; }
        
        [StringLength(200)]
        public string? ParasiteIdentification { get; set; }
        
        [StringLength(200)]
        public string? CryptosporidiumAntigen { get; set; }
        
        [StringLength(200)]
        public string? GiardiaAntigen { get; set; }
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public MicrobiologyInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum MicrobiologyInterpretation
    {
        NoGrowth = 1,
        NormalFlora = 2,
        PathogenIdentified = 3,
        MultipleOrganisms = 4,
        ResistantOrganism = 5,
        FungalInfection = 6,
        ParasiticInfection = 7,
        ContaminatedSpecimen = 8
    }
    
    // PCR Test Result
    public class PCRTestResult
    {
        [Key]
        public int PCRResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        [StringLength(100)]
        public string? TestType { get; set; } // COVID-19, Hepatitis, HIV, etc.
        
        [StringLength(200)]
        public string? TargetGene { get; set; }
        
        [StringLength(200)]
        public string? PrimerSet { get; set; }
        
        public string? QualitativeResult { get; set; } // Detected/Not Detected
        
        public double? QuantitativeResult { get; set; } // copies/mL or IU/mL
        
        public double? CycleThreshold { get; set; } // Ct value
        
        public double? ViralLoad { get; set; }
        
        [StringLength(100)]
        public string? Genotype { get; set; }
        
        [StringLength(100)]
        public string? Mutation { get; set; }
        
        // Quality Control
        [StringLength(200)]
        public string? InternalControl { get; set; }
        
        [StringLength(200)]
        public string? PositiveControl { get; set; }
        
        [StringLength(200)]
        public string? NegativeControl { get; set; }
        
        // Specific Tests
        [StringLength(200)]
        public string? COVID19Result { get; set; }
        
        [StringLength(200)]
        public string? InfluenzaAResult { get; set; }
        
        [StringLength(200)]
        public string? InfluenzaBResult { get; set; }
        
        [StringLength(200)]
        public string? RSVResult { get; set; }
        
        [StringLength(200)]
        public string? HepatitisBResult { get; set; }
        
        [StringLength(200)]
        public string? HepatitisCResult { get; set; }
        
        [StringLength(200)]
        public string? HIVResult { get; set; }
        
        [StringLength(200)]
        public string? CMVResult { get; set; }
        
        [StringLength(200)]
        public string? EBVResult { get; set; }
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public PCRInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum PCRInterpretation
    {
        NotDetected = 1,
        Detected = 2,
        Inconclusive = 3,
        HighViralLoad = 4,
        LowViralLoad = 5,
        InvalidResult = 6,
        ResistanceMutation = 7
    }
    
    // Serology Test Result
    public class SerologyTestResult
    {
        [Key]
        public int SerologyResultId { get; set; }
        
        [Required]
        public int ExamId { get; set; }
        
        // Hepatitis Panel
        [StringLength(100)]
        public string? HBsAg { get; set; } // Hepatitis B surface antigen
        
        [StringLength(100)]
        public string? AntiHBs { get; set; } // Hepatitis B surface antibody
        
        [StringLength(100)]
        public string? HBeAg { get; set; } // Hepatitis B e antigen
        
        [StringLength(100)]
        public string? AntiHBe { get; set; } // Hepatitis B e antibody
        
        [StringLength(100)]
        public string? AntiHBc { get; set; } // Hepatitis B core antibody
        
        [StringLength(100)]
        public string? AntiHCV { get; set; } // Hepatitis C antibody
        
        [StringLength(100)]
        public string? AntiHAV { get; set; } // Hepatitis A antibody
        
        // HIV Testing
        [StringLength(100)]
        public string? AntiHIV { get; set; } // HIV 1&2 antibodies
        
        [StringLength(100)]
        public string? HIVAntigen { get; set; } // HIV p24 antigen
        
        // Syphilis
        [StringLength(100)]
        public string? RPR { get; set; } // Rapid Plasma Reagin
        
        [StringLength(100)]
        public string? TPPA { get; set; } // Treponema pallidum particle agglutination
        
        [StringLength(100)]
        public string? FTAAbs { get; set; } // Fluorescent treponemal antibody absorption
        
        // TORCH Panel
        [StringLength(100)]
        public string? ToxoplasmaIgG { get; set; }
        
        [StringLength(100)]
        public string? ToxoplasmaIgM { get; set; }
        
        [StringLength(100)]
        public string? RubellaIgG { get; set; }
        
        [StringLength(100)]
        public string? RubellaIgM { get; set; }
        
        [StringLength(100)]
        public string? CMVIgG { get; set; }
        
        [StringLength(100)]
        public string? CMVIgM { get; set; }
        
        [StringLength(100)]
        public string? HSVIgG { get; set; }
        
        [StringLength(100)]
        public string? HSVIgM { get; set; }
        
        // Other Infections
        [StringLength(100)]
        public string? EBVIgG { get; set; }
        
        [StringLength(100)]
        public string? EBVIgM { get; set; }
        
        [StringLength(100)]
        public string? MycoplasmaisIgG { get; set; }
        
        [StringLength(100)]
        public string? MycoplasmaisIgM { get; set; }
        
        [StringLength(100)]
        public string? ChlamydiaIgG { get; set; }
        
        [StringLength(100)]
        public string? ChlamydiaIgM { get; set; }
        
        // Autoimmune Markers
        [StringLength(100)]
        public string? ANA { get; set; } // Antinuclear antibodies
        
        [StringLength(100)]
        public string? RheumatoidFactor { get; set; }
        
        [StringLength(100)]
        public string? AntiCCP { get; set; } // Anti-cyclic citrullinated peptide
        
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
        
        public int? AnalyzedByUserId { get; set; }
        
        [StringLength(500)]
        public string? TechnicianNotes { get; set; }
        
        public bool QualityControlPassed { get; set; } = true;
        
        [StringLength(500)]
        public string? QualityControlNotes { get; set; }
        
        public SerologyInterpretation? Interpretation { get; set; }
        
        [StringLength(1000)]
        public string? InterpretationNotes { get; set; }
        
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;
        
        [ForeignKey("AnalyzedByUserId")]
        public virtual User? AnalyzedByUser { get; set; }
    }
    
    public enum SerologyInterpretation
    {
        Negative = 1,
        Positive = 2,
        Indeterminate = 3,
        AcuteInfection = 4,
        ChronicInfection = 5,
        PastInfection = 6,
        Immunity = 7,
        Reactivation = 8
    }
}