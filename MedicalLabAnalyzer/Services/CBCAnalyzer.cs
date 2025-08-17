using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class CBCAnalyzer
    {
        private readonly ILogger<CBCAnalyzer> _logger;
        private readonly MedicalImageAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public CBCAnalyzer(
            ILogger<CBCAnalyzer> logger,
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
        
        public async Task<CBCTestResult> AnalyzeImageAsync(int examId, string imagePath, int analyzedByUserId, CBCAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation("بدء تحليل CBC للصورة: {ImagePath}", imagePath);
                
                // التحقق من وجود الملف
                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"ملف الصورة غير موجود: {imagePath}");
                
                // إعدادات التحليل الافتراضية
                settings ??= new CBCAnalysisSettings();
                
                // تحليل الصورة باستخدام AI
                var analysisResult = await _analysisService.AnalyzeCBCImageAsync(imagePath, settings);
                
                // إنشاء نتيجة CBC
                var cbcResult = new CBCTestResult
                {
                    ExamId = examId,
                    AnalyzedAt = DateTime.UtcNow,
                    AnalyzedByUserId = analyzedByUserId,
                    
                    // خلايا الدم الحمراء
                    RBCCount = analysisResult.RBCCount,
                    Hemoglobin = CalculateHemoglobin(analysisResult.RBCCount, analysisResult.RBCQuality),
                    Hematocrit = CalculateHematocrit(analysisResult.RBCCount, analysisResult.RBCVolume),
                    MCV = analysisResult.MCV,
                    MCH = CalculateMCH(analysisResult.RBCCount, analysisResult.Hemoglobin ?? 0),
                    MCHC = CalculateMCHC(analysisResult.Hemoglobin ?? 0, analysisResult.Hematocrit ?? 0),
                    RDW = analysisResult.RDW,
                    
                    // خلايا الدم البيضاء
                    WBCCount = analysisResult.WBCCount,
                    Neutrophils = analysisResult.Neutrophils,
                    Lymphocytes = analysisResult.Lymphocytes,
                    Monocytes = analysisResult.Monocytes,
                    Eosinophils = analysisResult.Eosinophils,
                    Basophils = analysisResult.Basophils,
                    
                    // الصفائح الدموية
                    PlateletCount = analysisResult.PlateletCount,
                    MPV = analysisResult.MPV,
                    
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
                cbcResult.Interpretation = InterpretCBCResults(cbcResult);
                cbcResult.IsAbnormal = CheckForAbnormalities(cbcResult);
                cbcResult.CriticalFlags = IdentifyCriticalValues(cbcResult);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveCBCResultAsync(cbcResult);
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تم تحليل CBC للفحص {examId}",
                    AuditActionType.Analysis,
                    analyzedByUserId,
                    $"CBC Analysis - RBC: {cbcResult.RBCCount:F2}, WBC: {cbcResult.WBCCount:F2}, PLT: {cbcResult.PlateletCount:F0}"
                );
                
                _logger.LogInformation("تم إكمال تحليل CBC بنجاح للفحص {ExamId}", examId);
                return cbcResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل CBC للفحص {ExamId}", examId);
                
                await _auditService.LogAsync(
                    $"فشل تحليل CBC للفحص {examId}: {ex.Message}",
                    AuditActionType.Error,
                    analyzedByUserId,
                    $"CBC Analysis Error: {ex.Message}"
                );
                
                throw;
            }
        }
        
        public async Task<CBCTestResult> ManualEntryAsync(int examId, CBCTestResult cbcData, int enteredByUserId)
        {
            try
            {
                _logger.LogInformation("إدخال يدوي لـ CBC للفحص {ExamId}", examId);
                
                cbcData.ExamId = examId;
                cbcData.AnalyzedAt = DateTime.UtcNow;
                cbcData.AnalyzedByUserId = enteredByUserId;
                cbcData.IsManualEntry = true;
                
                // تفسير النتائج
                cbcData.Interpretation = InterpretCBCResults(cbcData);
                cbcData.IsAbnormal = CheckForAbnormalities(cbcData);
                cbcData.CriticalFlags = IdentifyCriticalValues(cbcData);
                
                // حفظ في قاعدة البيانات
                await _dbService.SaveCBCResultAsync(cbcData);
                
                await _auditService.LogAsync(
                    $"تم إدخال CBC يدوياً للفحص {examId}",
                    AuditActionType.ManualEntry,
                    enteredByUserId,
                    $"Manual CBC Entry - RBC: {cbcData.RBCCount:F2}, WBC: {cbcData.WBCCount:F2}"
                );
                
                return cbcData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الإدخال اليدوي لـ CBC للفحص {ExamId}", examId);
                throw;
            }
        }
        
        private float CalculateHemoglobin(float? rbcCount, float? rbcQuality)
        {
            if (rbcCount == null) return 0;
            
            // معادلة تقريبية لحساب الهيموجلوبين بناءً على عدد وجودة خلايا الدم الحمراء
            var baseHgb = rbcCount.Value * 3.2f; // معامل تحويل تقريبي
            var qualityFactor = (rbcQuality ?? 1.0f);
            
            return baseHgb * qualityFactor;
        }
        
        private float CalculateHematocrit(float? rbcCount, float? rbcVolume)
        {
            if (rbcCount == null) return 0;
            
            var baseHct = rbcCount.Value * 3.0f; // معامل تحويل تقريبي
            var volumeFactor = (rbcVolume ?? 90) / 90.0f; // MCV طبيعي حوالي 90
            
            return baseHct * volumeFactor;
        }
        
        private float CalculateMCH(float? rbcCount, float hemoglobin)
        {
            if (rbcCount == null || rbcCount == 0) return 0;
            return (hemoglobin * 10) / rbcCount.Value;
        }
        
        private float CalculateMCHC(float hemoglobin, float hematocrit)
        {
            if (hematocrit == 0) return 0;
            return (hemoglobin * 100) / hematocrit;
        }
        
        private string InterpretCBCResults(CBCTestResult result)
        {
            var interpretations = new List<string>();
            
            // تفسير خلايا الدم الحمراء
            if (result.RBCCount < 4.0) interpretations.Add("انخفاض في عدد خلايا الدم الحمراء");
            else if (result.RBCCount > 5.5) interpretations.Add("ارتفاع في عدد خلايا الدم الحمراء");
            
            if (result.Hemoglobin < 12) interpretations.Add("انخفاض في الهيموجلوبين (فقر الدم)");
            else if (result.Hemoglobin > 16) interpretations.Add("ارتفاع في الهيموجلوبين");
            
            // تفسير خلايا الدم البيضاء
            if (result.WBCCount < 4.0) interpretations.Add("انخفاض في عدد خلايا الدم البيضاء");
            else if (result.WBCCount > 11.0) interpretations.Add("ارتفاع في عدد خلايا الدم البيضاء (قد يشير لعدوى)");
            
            // تفسير الصفائح الدموية
            if (result.PlateletCount < 150) interpretations.Add("انخفاض في عدد الصفائح الدموية");
            else if (result.PlateletCount > 450) interpretations.Add("ارتفاع في عدد الصفائح الدموية");
            
            return interpretations.Any() ? string.Join(". ", interpretations) : "النتائج ضمن المعدل الطبيعي";
        }
        
        private bool CheckForAbnormalities(CBCTestResult result)
        {
            return result.RBCCount < 4.0 || result.RBCCount > 5.5 ||
                   result.Hemoglobin < 12 || result.Hemoglobin > 16 ||
                   result.WBCCount < 4.0 || result.WBCCount > 11.0 ||
                   result.PlateletCount < 150 || result.PlateletCount > 450;
        }
        
        private string IdentifyCriticalValues(CBCTestResult result)
        {
            var criticalFlags = new List<string>();
            
            if (result.Hemoglobin < 7) criticalFlags.Add("CRITICAL: هيموجلوبين منخفض جداً");
            if (result.WBCCount < 1.0) criticalFlags.Add("CRITICAL: نقص شديد في خلايا الدم البيضاء");
            if (result.WBCCount > 30.0) criticalFlags.Add("CRITICAL: ارتفاع شديد في خلايا الدم البيضاء");
            if (result.PlateletCount < 50) criticalFlags.Add("CRITICAL: نقص شديد في الصفائح الدموية");
            
            return string.Join("; ", criticalFlags);
        }
        
        private string GenerateAnalysisNotes(CBCAnalysisResult analysisResult)
        {
            var notes = new List<string>
            {
                $"جودة الصورة: {analysisResult.QualityScore:F1}/10",
                $"ثقة النموذج: {analysisResult.ConfidenceScore:F1}%",
                $"وقت المعالجة: {analysisResult.ProcessingTimeMs}ms"
            };
            
            if (analysisResult.QualityScore < 7.0)
                notes.Add("تحذير: جودة الصورة منخفضة قد تؤثر على دقة النتائج");
            
            if (analysisResult.ConfidenceScore < 85.0)
                notes.Add("تحذير: ثقة النموذج منخفضة، يُنصح بالمراجعة اليدوية");
            
            return string.Join(". ", notes);
        }
    }
    
    public class CBCAnalysisSettings
    {
        public float MinCellSize { get; set; } = 5.0f;
        public float MaxCellSize { get; set; } = 50.0f;
        public float ConfidenceThreshold { get; set; } = 0.7f;
        public bool EnableQualityControl { get; set; } = true;
        public int SampleRegions { get; set; } = 5;
    }
    
    public class CBCAnalysisResult
    {
        public float? RBCCount { get; set; }
        public float? WBCCount { get; set; }
        public float? PlateletCount { get; set; }
        public float? Neutrophils { get; set; }
        public float? Lymphocytes { get; set; }
        public float? Monocytes { get; set; }
        public float? Eosinophils { get; set; }
        public float? Basophils { get; set; }
        public float? MCV { get; set; }
        public float? MPV { get; set; }
        public float? RDW { get; set; }
        public float? RBCQuality { get; set; }
        public float? RBCVolume { get; set; }
        public float? Hemoglobin { get; set; }
        public float? Hematocrit { get; set; }
        public float QualityScore { get; set; }
        public float ConfidenceScore { get; set; }
        public int ProcessingTimeMs { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
    }
}