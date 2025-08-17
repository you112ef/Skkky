using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class UrineAnalyzer
    {
        private readonly ILogger<UrineAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public UrineAnalyzer(
            ILogger<UrineAnalyzer> logger,
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
        
        public async Task<UrineTestResult> AnalyzeImageAsync(int examId, string imagePath, int analyzedByUserId, UrineAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل البول للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new UrineAnalysisSettings();
                
                // تحليل الصورة باستخدام AI
                var analysisResult = await _analysisService.AnalyzeUrineImageAsync(imagePath, settings);
                
                var urineResult = new UrineTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // الفحص الفيزيائي
                    Color = analysisResult.Color,
                    Clarity = analysisResult.Clarity,
                    Odor = analysisResult.Odor,
                    Volume = analysisResult.Volume,
                    SpecificGravity = analysisResult.SpecificGravity,
                    
                    // الفحص الكيميائي
                    pH = analysisResult.pH,
                    Protein = analysisResult.Protein,
                    Glucose = analysisResult.Glucose,
                    Ketones = analysisResult.Ketones,
                    Blood = analysisResult.Blood,
                    Bilirubin = analysisResult.Bilirubin,
                    Urobilinogen = analysisResult.Urobilinogen,
                    Nitrite = analysisResult.Nitrite,
                    LeukocyteEsterase = analysisResult.LeukocyteEsterase,
                    
                    // الفحص المجهري
                    RBCCount = analysisResult.RBCCount,
                    WBCCount = analysisResult.WBCCount,
                    EpithelialCells = analysisResult.EpithelialCells,
                    Bacteria = analysisResult.Bacteria,
                    Yeast = analysisResult.Yeast,
                    Crystals = analysisResult.Crystals,
                    Casts = analysisResult.Casts,
                    Mucus = analysisResult.Mucus,
                    
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
                urineResult.Interpretation = InterpretUrineResults(urineResult);
                urineResult.IsAbnormal = CheckForAbnormalities(urineResult);
                urineResult.CriticalFlags = IdentifyCriticalValues(urineResult);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveUrineResultAsync(urineResult);
                
                await _auditService.LogAsync(
                    $"تم تحليل البول للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Urine Analysis - RBC: {urineResult.RBCCount}, WBC: {urineResult.WBCCount}, Protein: {urineResult.Protein}"
                );
                
                _logger.LogInformation("تم إكمال تحليل البول بنجاح للفحص {ExamId}", examId);
                return urineResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل البول للفحص {ExamId}", examId);
                
                await _auditService.LogAsync(
                    $"فشل تحليل البول للفحص {examId}: {ex.Message}",
                    AuditActionType.Error,
                    analyzedByUserId,
                    $"Urine Analysis Error: {ex.Message}"
                );
                
                throw;
            }
        }
        
        public async Task<UrineTestResult> ManualEntryAsync(int examId, UrineTestResult urineData, int enteredByUserId)
        {
            try
            {
                _logger.LogInformation("إدخال يدوي لتحليل البول للفحص {ExamId}", examId);
                
                urineData.ExamId = examId;
                urineData.AnalyzedAt = DateTime.UtcNow;
                urineData.AnalyzedByUserId = enteredByUserId;
                urineData.IsManualEntry = true;
                
                // تفسير النتائج
                urineData.Interpretation = InterpretUrineResults(urineData);
                urineData.IsAbnormal = CheckForAbnormalities(urineData);
                urineData.CriticalFlags = IdentifyCriticalValues(urineData);
                
                await _dbService.SaveUrineResultAsync(urineData);
                
                await _auditService.LogAsync(
                    $"تم إدخال تحليل البول يدوياً للفحص {examId}",
                    AuditActionType.ManualEntry,
                    enteredByUserId,
                    $"Manual Urine Entry - Protein: {urineData.Protein}, Glucose: {urineData.Glucose}"
                );
                
                return urineData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الإدخال اليدوي لتحليل البول للفحص {ExamId}", examId);
                throw;
            }
        }
        
        private string InterpretUrineResults(UrineTestResult result)
        {
            var interpretations = new List<string>();
            
            // الفحص الفيزيائي
            if (result.Color == "أحمر" || result.Color == "بني")
                interpretations.Add("لون البول غير طبيعي قد يشير لوجود دم");
            
            if (result.Clarity == "عكر")
                interpretations.Add("البول عكر قد يشير لوجود عدوى أو بروتين");
            
            // الفحص الكيميائي
            if (result.Protein == "موجب")
                interpretations.Add("وجود بروتين في البول قد يشير لمشاكل في الكلى");
            
            if (result.Glucose == "موجب")
                interpretations.Add("وجود سكر في البول قد يشير لمرض السكري");
            
            if (result.Blood == "موجب")
                interpretations.Add("وجود دم في البول يحتاج لتقييم إضافي");
            
            if (result.Nitrite == "موجب")
                interpretations.Add("اختبار النيتريت موجب يشير لعدوى بكتيرية");
            
            if (result.LeukocyteEsterase == "موجب")
                interpretations.Add("وجود كريات بيضاء يشير لالتهاب في الجهاز البولي");
            
            // الفحص المجهري
            if (result.RBCCount > 5)
                interpretations.Add("عدد كريات الدم الحمراء مرتفع");
            
            if (result.WBCCount > 10)
                interpretations.Add("عدد كريات الدم البيضاء مرتفع يشير لالتهاب");
            
            if (result.Bacteria == "كثير")
                interpretations.Add("وجود بكتيريا بكثرة يشير لعدوى");
            
            if (result.Casts == "موجود")
                interpretations.Add("وجود أسطوانات قد يشير لمشاكل كلوية");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "نتائج البول ضمن المعدل الطبيعي";
        }
        
        private bool CheckForAbnormalities(UrineTestResult result)
        {
            return result.Protein == "موجب" ||
                   result.Glucose == "موجب" ||
                   result.Blood == "موجب" ||
                   result.Nitrite == "موجب" ||
                   result.LeukocyteEsterase == "موجب" ||
                   result.RBCCount > 5 ||
                   result.WBCCount > 10 ||
                   result.Bacteria == "كثير";
        }
        
        private string IdentifyCriticalValues(UrineTestResult result)
        {
            var criticalFlags = new List<string>();
            
            if (result.RBCCount > 20)
                criticalFlags.Add("CRITICAL: كريات دم حمراء كثيرة جداً");
            
            if (result.WBCCount > 50)
                criticalFlags.Add("CRITICAL: كريات دم بيضاء كثيرة جداً");
            
            if (result.Protein == "موجب بقوة")
                criticalFlags.Add("CRITICAL: بروتين عالي جداً");
            
            if (result.Glucose == "موجب بقوة")
                criticalFlags.Add("CRITICAL: سكر عالي جداً في البول");
            
            return string.Join("; ", criticalFlags);
        }
        
        private string GenerateAnalysisNotes(UrineAnalysisResult analysisResult)
        {
            var notes = new List<string>
            {
                $"جودة الصورة: {analysisResult.QualityScore:F1}/10",
                $"ثقة النموذج: {analysisResult.ConfidenceScore:F1}%",
                $"وقت المعالجة: {analysisResult.ProcessingTimeMs}ms"
            };
            
            if (analysisResult.QualityScore < 7.0)
                notes.Add("تحذير: جودة الصورة منخفضة");
            
            if (analysisResult.ConfidenceScore < 80.0)
                notes.Add("تحذير: ثقة النموذج منخفضة، يُنصح بالمراجعة");
            
            return string.Join(". ", notes);
        }
    }
    
    public class UrineAnalysisSettings
    {
        public float ConfidenceThreshold { get; set; } = 0.75f;
        public bool EnableMicroscopicAnalysis { get; set; } = true;
        public bool EnableColorAnalysis { get; set; } = true;
        public int SampleFields { get; set; } = 10;
    }
    
    public class UrineAnalysisResult
    {
        // الفحص الفيزيائي
        public string Color { get; set; } = string.Empty;
        public string Clarity { get; set; } = string.Empty;
        public string Odor { get; set; } = string.Empty;
        public float? Volume { get; set; }
        public float? SpecificGravity { get; set; }
        
        // الفحص الكيميائي
        public float? pH { get; set; }
        public string Protein { get; set; } = string.Empty;
        public string Glucose { get; set; } = string.Empty;
        public string Ketones { get; set; } = string.Empty;
        public string Blood { get; set; } = string.Empty;
        public string Bilirubin { get; set; } = string.Empty;
        public string Urobilinogen { get; set; } = string.Empty;
        public string Nitrite { get; set; } = string.Empty;
        public string LeukocyteEsterase { get; set; } = string.Empty;
        
        // الفحص المجهري
        public int RBCCount { get; set; }
        public int WBCCount { get; set; }
        public string EpithelialCells { get; set; } = string.Empty;
        public string Bacteria { get; set; } = string.Empty;
        public string Yeast { get; set; } = string.Empty;
        public string Crystals { get; set; } = string.Empty;
        public string Casts { get; set; } = string.Empty;
        public string Mucus { get; set; } = string.Empty;
        
        // معلومات التحليل
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
}