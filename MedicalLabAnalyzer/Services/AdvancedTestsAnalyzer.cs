using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class AdvancedTestsAnalyzer
    {
        private readonly ILogger<AdvancedTestsAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public AdvancedTestsAnalyzer(
            ILogger<AdvancedTestsAnalyzer> logger,
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
        
        #region Liver Function Tests
        
        public async Task<LiverFunctionTestResult> AnalyzeLiverFunctionAsync(int examId, string imagePath, int analyzedByUserId, LiverFunctionSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل وظائف الكبد للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new LiverFunctionSettings();
                var analysisResult = await _analysisService.AnalyzeLiverFunctionImageAsync(imagePath, settings);
                
                var result = new LiverFunctionTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // إنزيمات الكبد
                    ALT = analysisResult.ALT,
                    AST = analysisResult.AST,
                    ALP = analysisResult.ALP,
                    GGT = analysisResult.GGT,
                    
                    // البروتينات
                    TotalProtein = analysisResult.TotalProtein,
                    Albumin = analysisResult.Albumin,
                    Globulin = analysisResult.Globulin,
                    AlbuminGlobulinRatio = CalculateAGRatio(analysisResult.Albumin, analysisResult.Globulin),
                    
                    // البيليروبين
                    TotalBilirubin = analysisResult.TotalBilirubin,
                    DirectBilirubin = analysisResult.DirectBilirubin,
                    IndirectBilirubin = CalculateIndirectBilirubin(analysisResult.TotalBilirubin, analysisResult.DirectBilirubin),
                    
                    // معلومات التحليل
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateLiverFunctionNotes(analysisResult)
                };
                
                result.Interpretation = InterpretLiverFunction(result);
                result.IsAbnormal = CheckLiverAbnormalities(result);
                result.CriticalFlags = IdentifyLiverCriticalValues(result);
                
                await _dbService.SaveLiverFunctionResultAsync(result);
                
                await _auditService.LogAsync(
                    $"تم تحليل وظائف الكبد للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Liver Function - ALT: {result.ALT}, AST: {result.AST}"
                );
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل وظائف الكبد للفحص {ExamId}", examId);
                throw;
            }
        }
        
        #endregion
        
        #region Kidney Function Tests
        
        public async Task<KidneyFunctionTestResult> AnalyzeKidneyFunctionAsync(int examId, string imagePath, int analyzedByUserId, KidneyFunctionSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل وظائف الكلى للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new KidneyFunctionSettings();
                var analysisResult = await _analysisService.AnalyzeKidneyFunctionImageAsync(imagePath, settings);
                
                var result = new KidneyFunctionTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // وظائف الكلى
                    Creatinine = analysisResult.Creatinine,
                    BUN = analysisResult.BUN,
                    BUNCreatinineRatio = CalculateBUNCreatinineRatio(analysisResult.BUN, analysisResult.Creatinine),
                    eGFR = CalculateEGFR(analysisResult.Creatinine, settings.PatientAge, settings.PatientGender),
                    
                    // حمض اليوريك
                    UricAcid = analysisResult.UricAcid,
                    
                    // إضافية
                    Cystatin = analysisResult.Cystatin,
                    
                    // معلومات التحليل
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateKidneyFunctionNotes(analysisResult, settings)
                };
                
                result.Interpretation = InterpretKidneyFunction(result);
                result.IsAbnormal = CheckKidneyAbnormalities(result);
                result.CriticalFlags = IdentifyKidneyCriticalValues(result);
                
                await _dbService.SaveKidneyFunctionResultAsync(result);
                
                await _auditService.LogAsync(
                    $"تم تحليل وظائف الكلى للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Kidney Function - Creatinine: {result.Creatinine}, eGFR: {result.eGFR}"
                );
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل وظائف الكلى للفحص {ExamId}", examId);
                throw;
            }
        }
        
        #endregion
        
        #region Thyroid Function Tests
        
        public async Task<ThyroidTestResult> AnalyzeThyroidFunctionAsync(int examId, string imagePath, int analyzedByUserId, ThyroidSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل وظائف الغدة الدرقية للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new ThyroidSettings();
                var analysisResult = await _analysisService.AnalyzeThyroidImageAsync(imagePath, settings);
                
                var result = new ThyroidTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // هرمونات الغدة الدرقية
                    TSH = analysisResult.TSH,
                    T3 = analysisResult.T3,
                    T4 = analysisResult.T4,
                    FreeT3 = analysisResult.FreeT3,
                    FreeT4 = analysisResult.FreeT4,
                    
                    // الأجسام المضادة
                    AntiTPO = analysisResult.AntiTPO,
                    AntiTG = analysisResult.AntiTG,
                    TSI = analysisResult.TSI,
                    
                    // معلومات التحليل
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateThyroidNotes(analysisResult)
                };
                
                result.Interpretation = InterpretThyroidFunction(result);
                result.IsAbnormal = CheckThyroidAbnormalities(result);
                result.CriticalFlags = IdentifyThyroidCriticalValues(result);
                
                await _dbService.SaveThyroidResultAsync(result);
                
                await _auditService.LogAsync(
                    $"تم تحليل وظائف الغدة الدرقية للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"Thyroid - TSH: {result.TSH}, T4: {result.T4}"
                );
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل وظائف الغدة الدرقية للفحص {ExamId}", examId);
                throw;
            }
        }
        
        #endregion
        
        #region CRP Test
        
        public async Task<CRPTestResult> AnalyzeCRPAsync(int examId, string imagePath, int analyzedByUserId, CRPSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل CRP للصورة: {ImagePath}", imagePath);
                
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                settings ??= new CRPSettings();
                var analysisResult = await _analysisService.AnalyzeCRPImageAsync(imagePath, settings);
                
                var result = new CRPTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // قياسات CRP
                    CRPLevel = analysisResult.CRPLevel,
                    HighSensitivityCRP = analysisResult.HighSensitivityCRP,
                    
                    // معلومات التحليل
                    TestType = settings.TestType,
                    ImagePath = imagePath,
                    ThumbnailPath = await _mediaService.CreateThumbnailAsync(imagePath),
                    ProcessingTimeMs = analysisResult.ProcessingTimeMs,
                    AIConfidenceScore = analysisResult.ConfidenceScore,
                    AIModelVersion = analysisResult.ModelVersion,
                    QualityScore = analysisResult.QualityScore,
                    Notes = GenerateCRPNotes(analysisResult, settings)
                };
                
                result.Interpretation = InterpretCRP(result);
                result.IsAbnormal = CheckCRPAbnormalities(result);
                result.CriticalFlags = IdentifyCRPCriticalValues(result);
                
                await _dbService.SaveCRPResultAsync(result);
                
                await _auditService.LogAsync(
                    $"تم تحليل CRP للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"CRP - Level: {result.CRPLevel} mg/L"
                );
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل CRP للفحص {ExamId}", examId);
                throw;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private float? CalculateAGRatio(float? albumin, float? globulin)
        {
            if (!albumin.HasValue || !globulin.HasValue || globulin.Value == 0)
                return null;
            return albumin.Value / globulin.Value;
        }
        
        private float? CalculateIndirectBilirubin(float? total, float? direct)
        {
            if (!total.HasValue || !direct.HasValue)
                return null;
            return total.Value - direct.Value;
        }
        
        private float? CalculateBUNCreatinineRatio(float? bun, float? creatinine)
        {
            if (!bun.HasValue || !creatinine.HasValue || creatinine.Value == 0)
                return null;
            return bun.Value / creatinine.Value;
        }
        
        private float? CalculateEGFR(float? creatinine, int? age, string? gender)
        {
            if (!creatinine.HasValue || !age.HasValue || string.IsNullOrEmpty(gender))
                return null;
            
            // معادلة CKD-EPI المبسطة
            var k = gender.ToLower() == "أنثى" ? 0.7f : 0.9f;
            var a = gender.ToLower() == "أنثى" ? -0.329f : -0.411f;
            var genderFactor = gender.ToLower() == "أنثى" ? 1.018f : 1.0f;
            
            var ratio = creatinine.Value / k;
            var minRatio = Math.Min(ratio, 1.0f);
            var maxRatio = Math.Max(ratio, 1.0f);
            
            var egfr = 141.0f * (float)Math.Pow(minRatio, a) * (float)Math.Pow(maxRatio, -1.209f) * 
                      (float)Math.Pow(0.993f, age.Value) * genderFactor;
            
            return egfr;
        }
        
        private string InterpretLiverFunction(LiverFunctionTestResult result)
        {
            var interpretations = new List<string>();
            
            if (result.ALT > 40) interpretations.Add("ارتفاع في إنزيم ALT يشير لتلف خلايا الكبد");
            if (result.AST > 40) interpretations.Add("ارتفاع في إنزيم AST");
            if (result.ALP > 147) interpretations.Add("ارتفاع في إنزيم ALP");
            if (result.TotalBilirubin > 1.2) interpretations.Add("ارتفاع في البيليروبين");
            if (result.Albumin < 3.5) interpretations.Add("انخفاض في الألبومين");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "وظائف الكبد ضمن المعدل الطبيعي";
        }
        
        private bool CheckLiverAbnormalities(LiverFunctionTestResult result)
        {
            return result.ALT > 40 || result.AST > 40 || result.ALP > 147 || 
                   result.TotalBilirubin > 1.2 || result.Albumin < 3.5;
        }
        
        private string IdentifyLiverCriticalValues(LiverFunctionTestResult result)
        {
            var criticals = new List<string>();
            if (result.ALT > 200) criticals.Add("CRITICAL: ALT مرتفع جداً");
            if (result.AST > 200) criticals.Add("CRITICAL: AST مرتفع جداً");
            if (result.TotalBilirubin > 5.0) criticals.Add("CRITICAL: بيليروبين مرتفع جداً");
            if (result.Albumin < 2.0) criticals.Add("CRITICAL: ألبومين منخفض جداً");
            return string.Join("; ", criticals);
        }
        
        private string InterpretKidneyFunction(KidneyFunctionTestResult result)
        {
            var interpretations = new List<string>();
            
            if (result.Creatinine > 1.3) interpretations.Add("ارتفاع في الكرياتينين يشير لضعف وظائف الكلى");
            if (result.BUN > 23) interpretations.Add("ارتفاع في اليوريا");
            if (result.eGFR < 60) interpretations.Add("انخفاض في معدل الترشيح الكلوي");
            if (result.UricAcid > 7.0) interpretations.Add("ارتفاع في حمض اليوريك");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "وظائف الكلى ضمن المعدل الطبيعي";
        }
        
        private bool CheckKidneyAbnormalities(KidneyFunctionTestResult result)
        {
            return result.Creatinine > 1.3 || result.BUN > 23 || result.eGFR < 60 || result.UricAcid > 7.0;
        }
        
        private string IdentifyKidneyCriticalValues(KidneyFunctionTestResult result)
        {
            var criticals = new List<string>();
            if (result.Creatinine > 4.0) criticals.Add("CRITICAL: كرياتينين مرتفع جداً");
            if (result.eGFR < 15) criticals.Add("CRITICAL: فشل كلوي شديد");
            if (result.BUN > 80) criticals.Add("CRITICAL: يوريا مرتفعة جداً");
            return string.Join("; ", criticals);
        }
        
        private string InterpretThyroidFunction(ThyroidTestResult result)
        {
            var interpretations = new List<string>();
            
            if (result.TSH < 0.4) interpretations.Add("انخفاض TSH قد يشير لفرط نشاط الدرقية");
            else if (result.TSH > 4.0) interpretations.Add("ارتفاع TSH قد يشير لقصور الدرقية");
            
            if (result.FreeT4 < 0.8) interpretations.Add("انخفاض في T4 الحر");
            else if (result.FreeT4 > 1.8) interpretations.Add("ارتفاع في T4 الحر");
            
            if (result.AntiTPO > 35) interpretations.Add("وجود أجسام مضادة لـ TPO");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "وظائف الغدة الدرقية طبيعية";
        }
        
        private bool CheckThyroidAbnormalities(ThyroidTestResult result)
        {
            return result.TSH < 0.4 || result.TSH > 4.0 || result.FreeT4 < 0.8 || 
                   result.FreeT4 > 1.8 || result.AntiTPO > 35;
        }
        
        private string IdentifyThyroidCriticalValues(ThyroidTestResult result)
        {
            var criticals = new List<string>();
            if (result.TSH < 0.1) criticals.Add("CRITICAL: TSH منخفض جداً");
            if (result.TSH > 20) criticals.Add("CRITICAL: TSH مرتفع جداً");
            if (result.FreeT4 > 4.0) criticals.Add("CRITICAL: T4 مرتفع جداً");
            return string.Join("; ", criticals);
        }
        
        private string InterpretCRP(CRPTestResult result)
        {
            var level = result.CRPLevel ?? result.HighSensitivityCRP ?? 0;
            
            if (level < 1.0) return "مستوى CRP منخفض - خطر قلبي منخفض";
            else if (level < 3.0) return "مستوى CRP متوسط - خطر قلبي متوسط";
            else if (level < 10.0) return "مستوى CRP مرتفع - خطر قلبي مرتفع أو التهاب خفيف";
            else return "مستوى CRP مرتفع جداً - التهاب نشط";
        }
        
        private bool CheckCRPAbnormalities(CRPTestResult result)
        {
            var level = result.CRPLevel ?? result.HighSensitivityCRP ?? 0;
            return level > 3.0;
        }
        
        private string IdentifyCRPCriticalValues(CRPTestResult result)
        {
            var level = result.CRPLevel ?? result.HighSensitivityCRP ?? 0;
            return level > 50 ? "CRITICAL: CRP مرتفع جداً - التهاب شديد" : string.Empty;
        }
        
        private string GenerateLiverFunctionNotes(LiverFunctionAnalysisResult result)
        {
            return $"جودة: {result.QualityScore:F1}/10, ثقة: {result.ConfidenceScore:F1}%, معالجة: {result.ProcessingTimeMs}ms";
        }
        
        private string GenerateKidneyFunctionNotes(KidneyFunctionAnalysisResult result, KidneyFunctionSettings settings)
        {
            return $"العمر: {settings.PatientAge}, الجنس: {settings.PatientGender}, جودة: {result.QualityScore:F1}/10";
        }
        
        private string GenerateThyroidNotes(ThyroidAnalysisResult result)
        {
            return $"جودة: {result.QualityScore:F1}/10, ثقة: {result.ConfidenceScore:F1}%, معالجة: {result.ProcessingTimeMs}ms";
        }
        
        private string GenerateCRPNotes(CRPAnalysisResult result, CRPSettings settings)
        {
            return $"نوع الاختبار: {settings.TestType}, جودة: {result.QualityScore:F1}/10, ثقة: {result.ConfidenceScore:F1}%";
        }
        
        #endregion
    }
    
    #region Settings Classes
    
    public class LiverFunctionSettings
    {
        public float ConfidenceThreshold { get; set; } = 0.85f;
    }
    
    public class KidneyFunctionSettings
    {
        public float ConfidenceThreshold { get; set; } = 0.85f;
        public int? PatientAge { get; set; }
        public string? PatientGender { get; set; }
    }
    
    public class ThyroidSettings
    {
        public float ConfidenceThreshold { get; set; } = 0.85f;
    }
    
    public class CRPSettings
    {
        public string TestType { get; set; } = "High-Sensitivity"; // Standard, High-Sensitivity
        public float ConfidenceThreshold { get; set; } = 0.85f;
    }
    
    #endregion
    
    #region Analysis Result Classes
    
    public class LiverFunctionAnalysisResult
    {
        public float? ALT { get; set; }
        public float? AST { get; set; }
        public float? ALP { get; set; }
        public float? GGT { get; set; }
        public float? TotalProtein { get; set; }
        public float? Albumin { get; set; }
        public float? Globulin { get; set; }
        public float? TotalBilirubin { get; set; }
        public float? DirectBilirubin { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
    
    public class KidneyFunctionAnalysisResult
    {
        public float? Creatinine { get; set; }
        public float? BUN { get; set; }
        public float? UricAcid { get; set; }
        public float? Cystatin { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
    
    public class ThyroidAnalysisResult
    {
        public float? TSH { get; set; }
        public float? T3 { get; set; }
        public float? T4 { get; set; }
        public float? FreeT3 { get; set; }
        public float? FreeT4 { get; set; }
        public float? AntiTPO { get; set; }
        public float? AntiTG { get; set; }
        public float? TSI { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
    
    public class CRPAnalysisResult
    {
        public float? CRPLevel { get; set; }
        public float? HighSensitivityCRP { get; set; }
        
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
    
    #endregion
}