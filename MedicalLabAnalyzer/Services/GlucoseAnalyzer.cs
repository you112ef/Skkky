using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class GlucoseAnalyzer
    {
        private readonly ILogger<GlucoseAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public GlucoseAnalyzer(
            ILogger<GlucoseAnalyzer> logger,
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
        
        public async Task<GlucoseTestResult> AnalyzeImageAsync(int examId, string imagePath, int analyzedByUserId, GlucoseAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل السكر للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new GlucoseAnalysisSettings();
                
                // تحليل الصورة باستخدام AI (قراءة جهاز قياس السكر أو شريط الاختبار)
                var analysisResult = await _analysisService.AnalyzeGlucoseImageAsync(imagePath, settings);
                
                var glucoseResult = new GlucoseTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // قياسات السكر
                    FastingGlucose = analysisResult.FastingGlucose,
                    RandomGlucose = analysisResult.RandomGlucose,
                    PostPrandialGlucose = analysisResult.PostPrandialGlucose,
                    HbA1c = analysisResult.HbA1c,
                    
                    // اختبار تحمل السكر
                    GTTBaseline = analysisResult.GTTBaseline,
                    GTT1Hour = analysisResult.GTT1Hour,
                    GTT2Hour = analysisResult.GTT2Hour,
                    GTT3Hour = analysisResult.GTT3Hour,
                    
                    // معلومات التحليل
                    TestType = settings.TestType,
                    SampleType = settings.SampleType,
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateAnalysisNotes(analysisResult, settings)
                };
                
                // تفسير النتائج
                glucoseResult.Interpretation = InterpretGlucoseResults(glucoseResult);
                glucoseResult.IsAbnormal = CheckForAbnormalities(glucoseResult);
                glucoseResult.CriticalFlags = IdentifyCriticalValues(glucoseResult);
                
                // تحديد التشخيص المحتمل
                glucoseResult.DiagnosticSuggestion = SuggestDiagnosis(glucoseResult);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveGlucoseResultAsync(glucoseResult);
                
                await _auditService.LogAsync(
                    $"تم تحليل السكر للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Glucose Analysis - Type: {glucoseResult.TestType}, Value: {GetPrimaryGlucoseValue(glucoseResult):F1} mg/dL"
                );
                
                _logger.LogInformation("تم إكمال تحليل السكر بنجاح للفحص {ExamId}", examId);
                return glucoseResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل السكر للفحص {ExamId}", examId);
                
                await _auditService.LogAsync(
                    $"فشل تحليل السكر للفحص {examId}: {ex.Message}",
                    AuditActionType.Error,
                    analyzedByUserId,
                    $"Glucose Analysis Error: {ex.Message}"
                );
                
                throw;
            }
        }
        
        public async Task<GlucoseTestResult> ManualEntryAsync(int examId, GlucoseTestResult glucoseData, int enteredByUserId)
        {
            try
            {
                _logger.LogInformation("إدخال يدوي لتحليل السكر للفحص {ExamId}", examId);
                
                glucoseData.ExamId = examId;
                glucoseData.AnalyzedAt = DateTime.UtcNow;
                glucoseData.AnalyzedByUserId = enteredByUserId;
                glucoseData.IsManualEntry = true;
                
                // تفسير النتائج
                glucoseData.Interpretation = InterpretGlucoseResults(glucoseData);
                glucoseData.IsAbnormal = CheckForAbnormalities(glucoseData);
                glucoseData.CriticalFlags = IdentifyCriticalValues(glucoseData);
                glucoseData.DiagnosticSuggestion = SuggestDiagnosis(glucoseData);
                
                await _dbService.SaveGlucoseResultAsync(glucoseData);
                
                await _auditService.LogAsync(
                    $"تم إدخال تحليل السكر يدوياً للفحص {examId}",
                    AuditActionType.ManualEntry,
                    enteredByUserId,
                    $"Manual Glucose Entry - Type: {glucoseData.TestType}"
                );
                
                return glucoseData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الإدخال اليدوي لتحليل السكر للفحص {ExamId}", examId);
                throw;
            }
        }
        
        private float GetPrimaryGlucoseValue(GlucoseTestResult result)
        {
            return result.FastingGlucose ?? result.RandomGlucose ?? result.PostPrandialGlucose ?? result.GTTBaseline ?? 0;
        }
        
        private string InterpretGlucoseResults(GlucoseTestResult result)
        {
            var interpretations = new List<string>();
            
            // تفسير السكر الصائم
            if (result.FastingGlucose.HasValue)
            {
                var fasting = result.FastingGlucose.Value;
                if (fasting < 70)
                    interpretations.Add("نقص سكر الدم الصائم (أقل من 70 mg/dL)");
                else if (fasting >= 70 && fasting < 100)
                    interpretations.Add("سكر الدم الصائم طبيعي (70-99 mg/dL)");
                else if (fasting >= 100 && fasting < 126)
                    interpretations.Add("ما قبل السكري - جلوكوز صائم معطل (100-125 mg/dL)");
                else if (fasting >= 126)
                    interpretations.Add("مرض السكري - سكر صائم مرتفع (≥126 mg/dL)");
            }
            
            // تفسير السكر العشوائي
            if (result.RandomGlucose.HasValue)
            {
                var random = result.RandomGlucose.Value;
                if (random < 140)
                    interpretations.Add("سكر الدم العشوائي طبيعي (أقل من 140 mg/dL)");
                else if (random >= 140 && random < 200)
                    interpretations.Add("سكر الدم العشوائي مرتفع نسبياً (140-199 mg/dL)");
                else if (random >= 200)
                    interpretations.Add("مرض السكري - سكر عشوائي مرتفع (≥200 mg/dL)");
            }
            
            // تفسير السكر بعد الأكل
            if (result.PostPrandialGlucose.HasValue)
            {
                var postMeal = result.PostPrandialGlucose.Value;
                if (postMeal < 140)
                    interpretations.Add("سكر الدم بعد الأكل طبيعي (أقل من 140 mg/dL)");
                else if (postMeal >= 140 && postMeal < 200)
                    interpretations.Add("ما قبل السكري - سكر مرتفع بعد الأكل (140-199 mg/dL)");
                else if (postMeal >= 200)
                    interpretations.Add("مرض السكري - سكر مرتفع جداً بعد الأكل (≥200 mg/dL)");
            }
            
            // تفسير الهيموجلوبين السكري
            if (result.HbA1c.HasValue)
            {
                var hba1c = result.HbA1c.Value;
                if (hba1c < 5.7)
                    interpretations.Add("الهيموجلوبين السكري طبيعي (أقل من 5.7%)");
                else if (hba1c >= 5.7 && hba1c < 6.5)
                    interpretations.Add("ما قبل السكري - هيموجلوبين سكري مرتفع (5.7-6.4%)");
                else if (hba1c >= 6.5)
                    interpretations.Add("مرض السكري - هيموجلوبين سكري مرتفع (≥6.5%)");
            }
            
            // تفسير اختبار تحمل السكر
            if (result.GTT2Hour.HasValue)
            {
                var gtt2h = result.GTT2Hour.Value;
                if (gtt2h < 140)
                    interpretations.Add("اختبار تحمل السكر طبيعي (ساعتان أقل من 140 mg/dL)");
                else if (gtt2h >= 140 && gtt2h < 200)
                    interpretations.Add("ما قبل السكري - تحمل السكر معطل (140-199 mg/dL)");
                else if (gtt2h >= 200)
                    interpretations.Add("مرض السكري - فشل تحمل السكر (≥200 mg/dL)");
            }
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "قراءة غير واضحة للسكر";
        }
        
        private bool CheckForAbnormalities(GlucoseTestResult result)
        {
            return (result.FastingGlucose.HasValue && (result.FastingGlucose < 70 || result.FastingGlucose >= 100)) ||
                   (result.RandomGlucose.HasValue && result.RandomGlucose >= 140) ||
                   (result.PostPrandialGlucose.HasValue && result.PostPrandialGlucose >= 140) ||
                   (result.HbA1c.HasValue && result.HbA1c >= 5.7) ||
                   (result.GTT2Hour.HasValue && result.GTT2Hour >= 140);
        }
        
        private string IdentifyCriticalValues(GlucoseTestResult result)
        {
            var criticalFlags = new List<string>();
            
            if (result.FastingGlucose.HasValue && result.FastingGlucose < 50)
                criticalFlags.Add("CRITICAL: نقص سكر خطير (أقل من 50 mg/dL)");
                
            if (result.RandomGlucose.HasValue && result.RandomGlucose > 400)
                criticalFlags.Add("CRITICAL: ارتفاع سكر خطير (أكثر من 400 mg/dL)");
                
            if (result.RandomGlucose.HasValue && result.RandomGlucose < 50)
                criticalFlags.Add("CRITICAL: نقص سكر خطير");
                
            if (result.HbA1c.HasValue && result.HbA1c > 10.0)
                criticalFlags.Add("CRITICAL: هيموجلوبين سكري مرتفع جداً (أكثر من 10%)");
            
            return string.Join("; ", criticalFlags);
        }
        
        private string SuggestDiagnosis(GlucoseTestResult result)
        {
            var diabeticCriteria = 0;
            var preDiabeticCriteria = 0;
            
            // معايير تشخيص السكري
            if (result.FastingGlucose >= 126) diabeticCriteria++;
            else if (result.FastingGlucose >= 100) preDiabeticCriteria++;
            
            if (result.RandomGlucose >= 200) diabeticCriteria++;
            
            if (result.PostPrandialGlucose >= 200) diabeticCriteria++;
            else if (result.PostPrandialGlucose >= 140) preDiabeticCriteria++;
            
            if (result.HbA1c >= 6.5) diabeticCriteria++;
            else if (result.HbA1c >= 5.7) preDiabeticCriteria++;
            
            if (result.GTT2Hour >= 200) diabeticCriteria++;
            else if (result.GTT2Hour >= 140) preDiabeticCriteria++;
            
            // تحديد التشخيص
            if (diabeticCriteria >= 1)
                return "يشير لمرض السكري - يحتاج تأكيد بفحص ثاني";
            else if (preDiabeticCriteria >= 1)
                return "يشير لما قبل السكري - يحتاج متابعة";
            else if (result.FastingGlucose < 70 || result.RandomGlucose < 70)
                return "يشير لنقص سكر الدم - يحتاج تقييم";
            else
                return "مستويات السكر ضمن المعدل الطبيعي";
        }
        
        private string GenerateAnalysisNotes(GlucoseAnalysisResult analysisResult, GlucoseAnalysisSettings settings)
        {
            var notes = new List<string>
            {
                $"نوع الاختبار: {settings.TestType}",
                $"نوع العينة: {settings.SampleType}",
                $"جودة القراءة: {analysisResult.QualityScore:F1}/10",
                $"ثقة النموذج: {analysisResult.ConfidenceScore:F1}%"
            };
            
            if (analysisResult.QualityScore < 8.0)
                notes.Add("تحذير: جودة قراءة الجهاز منخفضة");
            
            if (analysisResult.ConfidenceScore < 90.0)
                notes.Add("تحذير: ثقة منخفضة في قراءة الرقم");
            
            return string.Join(". ", notes);
        }
    }
    
    public class GlucoseAnalysisSettings
    {
        public string TestType { get; set; } = "صائم"; // صائم، عشوائي، بعد الأكل، تحمل السكر، HbA1c
        public string SampleType { get; set; } = "دم شعيري"; // دم شعيري، دم وريدي، بلازما
        public float ConfidenceThreshold { get; set; } = 0.85f;
        public bool EnableDigitalReading { get; set; } = true;
        public bool EnableStripReading { get; set; } = true;
    }
    
    public class GlucoseAnalysisResult
    {
        public float? FastingGlucose { get; set; }
        public float? RandomGlucose { get; set; }
        public float? PostPrandialGlucose { get; set; }
        public float? HbA1c { get; set; }
        
        public float? GTTBaseline { get; set; }
        public float? GTT1Hour { get; set; }
        public float? GTT2Hour { get; set; }
        public float? GTT3Hour { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public string DetectedDeviceType { get; set; } = string.Empty; // نوع الجهاز المكتشف
    }
}