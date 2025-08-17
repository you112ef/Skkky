using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class StoolAnalyzer
    {
        private readonly ILogger<StoolAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public StoolAnalyzer(
            ILogger<StoolAnalyzer> logger,
            MedicalImageAnalysisService analysisService,
            MediaService mediaService,
            DatabaseService dbService,
            AuditService auditService)
        {
            _logger = logger;
            _analysisService = analysisService;
            _mediaService = mediaService;
            _dbService = dbService;
            _auditService = auditService;
        }
        
        public async Task<StoolTestResult> AnalyzeImageAsync(int examId, string imagePath, int analyzedByUserId, StoolAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل البراز للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new StoolAnalysisSettings();
                
                // تحليل الصورة باستخدام AI
                var analysisResult = await _analysisService.AnalyzeStoolImageAsync(imagePath, settings);
                
                var stoolResult = new StoolTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // الفحص المجهري
                    Color = analysisResult.Color,
                    Consistency = analysisResult.Consistency,
                    Odor = analysisResult.Odor,
                    pH = analysisResult.pH,
                    Blood = analysisResult.Blood,
                    Mucus = analysisResult.Mucus,
                    Fat = analysisResult.Fat,
                    
                    // الطفيليات والميكروبات
                    Parasites = analysisResult.Parasites,
                    ParasiteDetails = analysisResult.ParasiteDetails,
                    Ova = analysisResult.Ova,
                    OvaDetails = analysisResult.OvaDetails,
                    Cysts = analysisResult.Cysts,
                    CystDetails = analysisResult.CystDetails,
                    
                    // البكتيريا والفطريات
                    Bacteria = analysisResult.Bacteria,
                    BacteriaTypes = analysisResult.BacteriaTypes,
                    Yeast = analysisResult.Yeast,
                    YeastTypes = analysisResult.YeastTypes,
                    
                    // خلايا الدم والالتهاب
                    RBCCount = analysisResult.RBCCount,
                    WBCCount = analysisResult.WBCCount,
                    EpithelialCells = analysisResult.EpithelialCells,
                    
                    // الألياف والمواد غير المهضومة
                    MuscularFibers = analysisResult.MuscularFibers,
                    VegetableFibers = analysisResult.VegetableFibers,
                    Starch = analysisResult.Starch,
                    UndigestedFood = analysisResult.UndigestedFood,
                    
                    // معلومات التحليل
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateAnalysisNotes(analysisResult)
                };
                
                // تفسير النتائج
                stoolResult.Interpretation = InterpretStoolResults(stoolResult);
                stoolResult.IsAbnormal = CheckForAbnormalities(stoolResult);
                stoolResult.CriticalFlags = IdentifyCriticalValues(stoolResult);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveStoolResultAsync(stoolResult);
                
                await _auditService.LogAsync(
                    $"تم تحليل البراز للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Stool Analysis - Parasites: {stoolResult.Parasites}, Blood: {stoolResult.Blood}"
                );
                
                _logger.LogInformation("تم إكمال تحليل البراز بنجاح للفحص {ExamId}", examId);
                return stoolResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل البراز للفحص {ExamId}", examId);
                
                await _auditService.LogAsync(
                    $"فشل تحليل البراز للفحص {examId}: {ex.Message}",
                    AuditActionType.Error,
                    analyzedByUserId,
                    $"Stool Analysis Error: {ex.Message}"
                );
                
                throw;
            }
        }
        
        public async Task<StoolTestResult> ManualEntryAsync(int examId, StoolTestResult stoolData, int enteredByUserId)
        {
            try
            {
                _logger.LogInformation("إدخال يدوي لتحليل البراز للفحص {ExamId}", examId);
                
                stoolData.ExamId = examId;
                stoolData.AnalyzedAt = DateTime.UtcNow;
                stoolData.AnalyzedByUserId = enteredByUserId;
                stoolData.IsManualEntry = true;
                
                // تفسير النتائج
                stoolData.Interpretation = InterpretStoolResults(stoolData);
                stoolData.IsAbnormal = CheckForAbnormalities(stoolData);
                stoolData.CriticalFlags = IdentifyCriticalValues(stoolData);
                
                await _dbService.SaveStoolResultAsync(stoolData);
                
                await _auditService.LogAsync(
                    $"تم إدخال تحليل البراز يدوياً للفحص {examId}",
                    AuditActionType.ManualEntry,
                    enteredByUserId,
                    $"Manual Stool Entry - Parasites: {stoolData.Parasites}"
                );
                
                return stoolData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الإدخال اليدوي لتحليل البراز للفحص {ExamId}", examId);
                throw;
            }
        }
        
        private string InterpretStoolResults(StoolTestResult result)
        {
            var interpretations = new List<string>();
            
            // الفحص الفيزيائي
            if (result.Blood == "موجب")
                interpretations.Add("وجود دم في البراز يحتاج لتقييم إضافي");
                
            if (result.Mucus == "كثير")
                interpretations.Add("وجود مخاط بكثرة قد يشير لالتهاب");
                
            if (result.Fat == "موجب")
                interpretations.Add("وجود دهون قد يشير لسوء امتصاص");
            
            // الطفيليات
            if (result.Parasites == "موجب")
            {
                interpretations.Add("تم العثور على طفيليات");
                if (!string.IsNullOrEmpty(result.ParasiteDetails))
                    interpretations.Add($"تفاصيل الطفيليات: {result.ParasiteDetails}");
            }
            
            if (result.Ova == "موجب")
            {
                interpretations.Add("تم العثور على بيوض طفيليات");
                if (!string.IsNullOrEmpty(result.OvaDetails))
                    interpretations.Add($"تفاصيل البيوض: {result.OvaDetails}");
            }
            
            if (result.Cysts == "موجب")
            {
                interpretations.Add("تم العثور على أكياس طفيليات");
                if (!string.IsNullOrEmpty(result.CystDetails))
                    interpretations.Add($"تفاصيل الأكياس: {result.CystDetails}");
            }
            
            // البكتيريا والفطريات
            if (result.Bacteria == "مرضية")
            {
                interpretations.Add("وجود بكتيريا مرضية");
                if (!string.IsNullOrEmpty(result.BacteriaTypes))
                    interpretations.Add($"أنواع البكتيريا: {result.BacteriaTypes}");
            }
            
            if (result.Yeast == "كثير")
            {
                interpretations.Add("وجود فطريات بكثرة");
                if (!string.IsNullOrEmpty(result.YeastTypes))
                    interpretations.Add($"أنواع الفطريات: {result.YeastTypes}");
            }
            
            // الالتهاب
            if (result.WBCCount > 10)
                interpretations.Add("وجود كريات دم بيضاء يشير لالتهاب");
                
            if (result.RBCCount > 5)
                interpretations.Add("وجود كريات دم حمراء");
            
            // الهضم
            if (result.MuscularFibers == "كثير")
                interpretations.Add("وجود ألياف عضلية كثيرة قد يشير لسوء هضم البروتين");
                
            if (result.Starch == "موجب")
                interpretations.Add("وجود نشا قد يشير لسوء هضم الكربوهيدرات");
                
            if (result.UndigestedFood == "كثير")
                interpretations.Add("وجود طعام غير مهضوم بكثرة");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "نتائج البراز ضمن المعدل الطبيعي";
        }
        
        private bool CheckForAbnormalities(StoolTestResult result)
        {
            return result.Blood == "موجب" ||
                   result.Parasites == "موجب" ||
                   result.Ova == "موجب" ||
                   result.Cysts == "موجب" ||
                   result.Bacteria == "مرضية" ||
                   result.WBCCount > 10 ||
                   result.RBCCount > 5 ||
                   result.Fat == "موجب";
        }
        
        private string IdentifyCriticalValues(StoolTestResult result)
        {
            var criticalFlags = new List<string>();
            
            if (result.Blood == "موجب بقوة")
                criticalFlags.Add("CRITICAL: دم كثير في البراز");
                
            if (result.Parasites == "موجب" && result.ParasiteDetails?.Contains("خطير") == true)
                criticalFlags.Add("CRITICAL: طفيليات خطيرة");
                
            if (result.WBCCount > 50)
                criticalFlags.Add("CRITICAL: التهاب شديد");
                
            if (result.RBCCount > 20)
                criticalFlags.Add("CRITICAL: كريات دم حمراء كثيرة جداً");
            
            return string.Join("; ", criticalFlags);
        }
        
        private string GenerateAnalysisNotes(StoolAnalysisResult analysisResult)
        {
            var notes = new List<string>
            {
                $"جودة الصورة: {analysisResult.QualityScore:F1}/10",
                $"ثقة النموذج: {analysisResult.ConfidenceScore:F1}%",
                $"وقت المعالجة: {analysisResult.ProcessingTimeMs}ms"
            };
            
            if (analysisResult.QualityScore < 6.0)
                notes.Add("تحذير: جودة الصورة منخفضة جداً");
            
            if (analysisResult.ConfidenceScore < 75.0)
                notes.Add("تحذير: ثقة النموذج منخفضة للطفيليات");
            
            return string.Join(". ", notes);
        }
    }
    
    public class StoolAnalysisSettings
    {
        public float ParasiteConfidenceThreshold { get; set; } = 0.8f;
        public float BacteriaConfidenceThreshold { get; set; } = 0.75f;
        public bool EnableParasiteDetection { get; set; } = true;
        public bool EnableBacteriaDetection { get; set; } = true;
        public int SampleFields { get; set; } = 15;
    }
    
    public class StoolAnalysisResult
    {
        // الفحص الفيزيائي
        public string Color { get; set; } = string.Empty;
        public string Consistency { get; set; } = string.Empty;
        public string Odor { get; set; } = string.Empty;
        public float? pH { get; set; }
        public string Blood { get; set; } = string.Empty;
        public string Mucus { get; set; } = string.Empty;
        public string Fat { get; set; } = string.Empty;
        
        // الطفيليات والميكروبات
        public string Parasites { get; set; } = string.Empty;
        public string ParasiteDetails { get; set; } = string.Empty;
        public string Ova { get; set; } = string.Empty;
        public string OvaDetails { get; set; } = string.Empty;
        public string Cysts { get; set; } = string.Empty;
        public string CystDetails { get; set; } = string.Empty;
        
        // البكتيريا والفطريات
        public string Bacteria { get; set; } = string.Empty;
        public string BacteriaTypes { get; set; } = string.Empty;
        public string Yeast { get; set; } = string.Empty;
        public string YeastTypes { get; set; } = string.Empty;
        
        // خلايا الدم والالتهاب
        public int RBCCount { get; set; }
        public int WBCCount { get; set; }
        public string EpithelialCells { get; set; } = string.Empty;
        
        // الألياف والمواد غير المهضومة
        public string MuscularFibers { get; set; } = string.Empty;
        public string VegetableFibers { get; set; } = string.Empty;
        public string Starch { get; set; } = string.Empty;
        public string UndigestedFood { get; set; } = string.Empty;
        
        // معلومات التحليل
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
}