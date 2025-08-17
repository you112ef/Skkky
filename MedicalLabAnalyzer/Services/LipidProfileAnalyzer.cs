using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class LipidProfileAnalyzer
    {
        private readonly ILogger<LipidProfileAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public LipidProfileAnalyzer(
            ILogger<LipidProfileAnalyzer> logger,
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
        
        public async Task<LipidProfileTestResult> AnalyzeImageAsync(int examId, string imagePath, int analyzedByUserId, LipidAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل ملف الدهون للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new LipidAnalysisSettings();
                
                // تحليل الصورة باستخدام AI (قراءة نتائج التحليل من تقرير أو جهاز)
                var analysisResult = await _analysisService.AnalyzeLipidProfileImageAsync(imagePath, settings);
                
                var lipidResult = new LipidProfileTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // قياسات الدهون الأساسية
                    TotalCholesterol = analysisResult.TotalCholesterol,
                    HDLCholesterol = analysisResult.HDLCholesterol,
                    LDLCholesterol = analysisResult.LDLCholesterol,
                    Triglycerides = analysisResult.Triglycerides,
                    VLDLCholesterol = analysisResult.VLDLCholesterol,
                    
                    // النسب والمؤشرات
                    CholesterolHDLRatio = CalculateCholesterolHDLRatio(analysisResult.TotalCholesterol, analysisResult.HDLCholesterol),
                    LDLHDLRatio = CalculateLDLHDLRatio(analysisResult.LDLCholesterol, analysisResult.HDLCholesterol),
                    NonHDLCholesterol = CalculateNonHDLCholesterol(analysisResult.TotalCholesterol, analysisResult.HDLCholesterol),
                    
                    // معلومات التحليل
                    FastingStatus = settings.FastingStatus,
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateAnalysisNotes(analysisResult, settings)
                };
                
                // حساب LDL إذا لم يكن متوفراً (معادلة Friedewald)
                if (!lipidResult.LDLCholesterol.HasValue && lipidResult.TotalCholesterol.HasValue && 
                    lipidResult.HDLCholesterol.HasValue && lipidResult.Triglycerides.HasValue && 
                    lipidResult.Triglycerides.Value < 400)
                {
                    lipidResult.LDLCholesterol = lipidResult.TotalCholesterol.Value - lipidResult.HDLCholesterol.Value - (lipidResult.Triglycerides.Value / 5);
                    lipidResult.Notes += ". تم حساب LDL باستخدام معادلة Friedewald";
                }
                
                // تفسير النتائج
                lipidResult.Interpretation = InterpretLipidResults(lipidResult);
                lipidResult.IsAbnormal = CheckForAbnormalities(lipidResult);
                lipidResult.CriticalFlags = IdentifyCriticalValues(lipidResult);
                lipidResult.RiskAssessment = AssessCardiovascularRisk(lipidResult);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveLipidProfileResultAsync(lipidResult);
                
                await _auditService.LogAsync(
                    $"تم تحليل ملف الدهون للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Lipid Profile - Total: {lipidResult.TotalCholesterol:F0}, LDL: {lipidResult.LDLCholesterol:F0}, HDL: {lipidResult.HDLCholesterol:F0}"
                );
                
                _logger.LogInformation("تم إكمال تحليل ملف الدهون بنجاح للفحص {ExamId}", examId);
                return lipidResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل ملف الدهون للفحص {ExamId}", examId);
                
                await _auditService.LogAsync(
                    $"فشل تحليل ملف الدهون للفحص {examId}: {ex.Message}",
                    AuditActionType.Error,
                    analyzedByUserId,
                    $"Lipid Profile Analysis Error: {ex.Message}"
                );
                
                throw;
            }
        }
        
        public async Task<LipidProfileTestResult> ManualEntryAsync(int examId, LipidProfileTestResult lipidData, int enteredByUserId)
        {
            try
            {
                _logger.LogInformation("إدخال يدوي لملف الدهون للفحص {ExamId}", examId);
                
                lipidData.ExamId = examId;
                lipidData.AnalyzedAt = DateTime.UtcNow;
                lipidData.AnalyzedByUserId = enteredByUserId;
                lipidData.IsManualEntry = true;
                
                // حساب النسب والمؤشرات
                lipidData.CholesterolHDLRatio = CalculateCholesterolHDLRatio(lipidData.TotalCholesterol, lipidData.HDLCholesterol);
                lipidData.LDLHDLRatio = CalculateLDLHDLRatio(lipidData.LDLCholesterol, lipidData.HDLCholesterol);
                lipidData.NonHDLCholesterol = CalculateNonHDLCholesterol(lipidData.TotalCholesterol, lipidData.HDLCholesterol);
                
                // تفسير النتائج
                lipidData.Interpretation = InterpretLipidResults(lipidData);
                lipidData.IsAbnormal = CheckForAbnormalities(lipidData);
                lipidData.CriticalFlags = IdentifyCriticalValues(lipidData);
                lipidData.RiskAssessment = AssessCardiovascularRisk(lipidData);
                
                await _dbService.SaveLipidProfileResultAsync(lipidData);
                
                await _auditService.LogAsync(
                    $"تم إدخال ملف الدهون يدوياً للفحص {examId}",
                    AuditActionType.ManualEntry,
                    enteredByUserId,
                    $"Manual Lipid Entry - Total Cholesterol: {lipidData.TotalCholesterol}"
                );
                
                return lipidData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الإدخال اليدوي لملف الدهون للفحص {ExamId}", examId);
                throw;
            }
        }
        
        private float? CalculateCholesterolHDLRatio(float? totalCholesterol, float? hdlCholesterol)
        {
            if (!totalCholesterol.HasValue || !hdlCholesterol.HasValue || hdlCholesterol.Value == 0)
                return null;
            return totalCholesterol.Value / hdlCholesterol.Value;
        }
        
        private float? CalculateLDLHDLRatio(float? ldlCholesterol, float? hdlCholesterol)
        {
            if (!ldlCholesterol.HasValue || !hdlCholesterol.HasValue || hdlCholesterol.Value == 0)
                return null;
            return ldlCholesterol.Value / hdlCholesterol.Value;
        }
        
        private float? CalculateNonHDLCholesterol(float? totalCholesterol, float? hdlCholesterol)
        {
            if (!totalCholesterol.HasValue || !hdlCholesterol.HasValue)
                return null;
            return totalCholesterol.Value - hdlCholesterol.Value;
        }
        
        private string InterpretLipidResults(LipidProfileTestResult result)
        {
            var interpretations = new List<string>();
            
            // تفسير الكوليسترول الكلي
            if (result.TotalCholesterol.HasValue)
            {
                var total = result.TotalCholesterol.Value;
                if (total < 200)
                    interpretations.Add("الكوليسترول الكلي مثالي (أقل من 200 mg/dL)");
                else if (total >= 200 && total < 240)
                    interpretations.Add("الكوليسترول الكلي حدي مرتفع (200-239 mg/dL)");
                else if (total >= 240)
                    interpretations.Add("الكوليسترول الكلي مرتفع (≥240 mg/dL)");
            }
            
            // تفسير الكوليسترول الضار (LDL)
            if (result.LDLCholesterol.HasValue)
            {
                var ldl = result.LDLCholesterol.Value;
                if (ldl < 100)
                    interpretations.Add("الكوليسترول الضار مثالي (أقل من 100 mg/dL)");
                else if (ldl >= 100 && ldl < 130)
                    interpretations.Add("الكوليسترول الضار قريب من المثالي (100-129 mg/dL)");
                else if (ldl >= 130 && ldl < 160)
                    interpretations.Add("الكوليسترول الضار حدي مرتفع (130-159 mg/dL)");
                else if (ldl >= 160 && ldl < 190)
                    interpretations.Add("الكوليسترول الضار مرتفع (160-189 mg/dL)");
                else if (ldl >= 190)
                    interpretations.Add("الكوليسترول الضار مرتفع جداً (≥190 mg/dL)");
            }
            
            // تفسير الكوليسترول النافع (HDL)
            if (result.HDLCholesterol.HasValue)
            {
                var hdl = result.HDLCholesterol.Value;
                if (hdl < 40)
                    interpretations.Add("الكوليسترول النافع منخفض - عامل خطر (أقل من 40 mg/dL للرجال)");
                else if (hdl >= 40 && hdl < 60)
                    interpretations.Add("الكوليسترول النافع ضمن المعدل الطبيعي (40-59 mg/dL)");
                else if (hdl >= 60)
                    interpretations.Add("الكوليسترول النافع مرتفع - عامل حماية (≥60 mg/dL)");
            }
            
            // تفسير الدهون الثلاثية
            if (result.Triglycerides.HasValue)
            {
                var tg = result.Triglycerides.Value;
                if (tg < 150)
                    interpretations.Add("الدهون الثلاثية طبيعية (أقل من 150 mg/dL)");
                else if (tg >= 150 && tg < 200)
                    interpretations.Add("الدهون الثلاثية حدية مرتفعة (150-199 mg/dL)");
                else if (tg >= 200 && tg < 500)
                    interpretations.Add("الدهون الثلاثية مرتفعة (200-499 mg/dL)");
                else if (tg >= 500)
                    interpretations.Add("الدهون الثلاثية مرتفعة جداً (≥500 mg/dL)");
            }
            
            // تفسير النسب
            if (result.CholesterolHDLRatio.HasValue)
            {
                var ratio = result.CholesterolHDLRatio.Value;
                if (ratio < 5.0)
                    interpretations.Add("نسبة الكوليسترول الكلي/HDL مقبولة (أقل من 5:1)");
                else
                    interpretations.Add("نسبة الكوليسترول الكلي/HDL مرتفعة - زيادة خطر القلب");
            }
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "ملف الدهون غير مكتمل";
        }
        
        private bool CheckForAbnormalities(LipidProfileTestResult result)
        {
            return (result.TotalCholesterol.HasValue && result.TotalCholesterol >= 200) ||
                   (result.LDLCholesterol.HasValue && result.LDLCholesterol >= 130) ||
                   (result.HDLCholesterol.HasValue && result.HDLCholesterol < 40) ||
                   (result.Triglycerides.HasValue && result.Triglycerides >= 150) ||
                   (result.CholesterolHDLRatio.HasValue && result.CholesterolHDLRatio >= 5.0);
        }
        
        private string IdentifyCriticalValues(LipidProfileTestResult result)
        {
            var criticalFlags = new List<string>();
            
            if (result.TotalCholesterol.HasValue && result.TotalCholesterol >= 300)
                criticalFlags.Add("CRITICAL: كوليسترول كلي مرتفع جداً (≥300 mg/dL)");
                
            if (result.LDLCholesterol.HasValue && result.LDLCholesterol >= 190)
                criticalFlags.Add("CRITICAL: كوليسترول ضار مرتفع جداً (≥190 mg/dL)");
                
            if (result.HDLCholesterol.HasValue && result.HDLCholesterol < 25)
                criticalFlags.Add("CRITICAL: كوليسترول نافع منخفض جداً (أقل من 25 mg/dL)");
                
            if (result.Triglycerides.HasValue && result.Triglycerides >= 500)
                criticalFlags.Add("CRITICAL: دهون ثلاثية مرتفعة جداً - خطر التهاب البنكرياس");
            
            return string.Join("; ", criticalFlags);
        }
        
        private string AssessCardiovascularRisk(LipidProfileTestResult result)
        {
            var riskFactors = new List<string>();
            var protectiveFactors = new List<string>();
            
            // عوامل الخطر
            if (result.TotalCholesterol >= 240)
                riskFactors.Add("كوليسترول كلي مرتفع");
                
            if (result.LDLCholesterol >= 160)
                riskFactors.Add("كوليسترول ضار مرتفع");
                
            if (result.HDLCholesterol < 40)
                riskFactors.Add("كوليسترول نافع منخفض");
                
            if (result.Triglycerides >= 200)
                riskFactors.Add("دهون ثلاثية مرتفعة");
                
            if (result.CholesterolHDLRatio >= 5.0)
                riskFactors.Add("نسبة كوليسترول كلي/HDL مرتفعة");
            
            // العوامل الوقائية
            if (result.HDLCholesterol >= 60)
                protectiveFactors.Add("كوليسترول نافع مرتفع");
                
            if (result.LDLCholesterol < 100)
                protectiveFactors.Add("كوليسترول ضار مثالي");
                
            if (result.TotalCholesterol < 200)
                protectiveFactors.Add("كوليسترول كلي مثالي");
            
            // تقييم الخطر الإجمالي
            var riskLevel = "منخفض";
            if (riskFactors.Count >= 3)
                riskLevel = "مرتفع";
            else if (riskFactors.Count >= 1)
                riskLevel = "متوسط";
            else if (protectiveFactors.Count >= 2)
                riskLevel = "منخفض جداً";
            
            var assessment = $"تقييم خطر أمراض القلب: {riskLevel}";
            
            if (riskFactors.Any())
                assessment += $". عوامل الخطر: {string.Join("، ", riskFactors)}";
                
            if (protectiveFactors.Any())
                assessment += $". عوامل الحماية: {string.Join("، ", protectiveFactors)}";
            
            return assessment;
        }
        
        private string GenerateAnalysisNotes(LipidAnalysisResult analysisResult, LipidAnalysisSettings settings)
        {
            var notes = new List<string>
            {
                $"حالة الصيام: {settings.FastingStatus}",
                $"جودة القراءة: {analysisResult.QualityScore:F1}/10",
                $"ثقة النموذج: {analysisResult.ConfidenceScore:F1}%",
                $"وقت المعالجة: {analysisResult.ProcessingTimeMs}ms"
            };
            
            if (settings.FastingStatus != "صائم 12 ساعة")
                notes.Add("تنبيه: يُفضل الصيام 12 ساعة للحصول على نتائج دقيقة للدهون الثلاثية");
            
            if (analysisResult.QualityScore < 8.0)
                notes.Add("تحذير: جودة قراءة التقرير منخفضة");
            
            if (analysisResult.ConfidenceScore < 85.0)
                notes.Add("تحذير: ثقة منخفضة في قراءة بعض القيم");
            
            return string.Join(". ", notes);
        }
    }
    
    public class LipidAnalysisSettings
    {
        public string FastingStatus { get; set; } = "صائم 12 ساعة"; // صائم، غير صائم، غير معروف
        public float ConfidenceThreshold { get; set; } = 0.85f;
        public bool EnableRatioCalculation { get; set; } = true;
        public bool EnableRiskAssessment { get; set; } = true;
    }
    
    public class LipidAnalysisResult
    {
        public float? TotalCholesterol { get; set; }
        public float? HDLCholesterol { get; set; }
        public float? LDLCholesterol { get; set; }
        public float? Triglycerides { get; set; }
        public float? VLDLCholesterol { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public string DetectedReportType { get; set; } = string.Empty; // نوع التقرير المكتشف
    }
}