using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.Services
{
    public class CASAAnalyzer
    {
        private readonly ILogger&lt;CASAAnalyzer&gt; _logger;
        private readonly CASAAnalysisService _analysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        public CASAAnalyzer(
            ILogger&lt;CASAAnalyzer&gt; logger,
            CASAAnalysisService analysisService,
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
        
        public async Task&lt;CASAResult&gt; AnalyzeVideoAsync(int examId, string videoPath, int analyzedByUserId, CASAAnalysisSettings? settings = null)
        {
            try
            {
                _logger.LogInformation($"Starting CASA analysis for exam {examId}");
                
                // Check if exam exists and get exam info
                var exam = await GetExamAsync(examId);
                if (exam == null)
                {
                    throw new ArgumentException($"Exam {examId} not found");
                }
                
                // Use default settings if not provided
                settings ??= new CASAAnalysisSettings();
                
                // Perform AI analysis
                var analysisResult = await _analysisService.AnalyzeVideoAsync(videoPath, settings);
                
                if (!analysisResult.IsCompleted)
                {
                    throw new Exception(analysisResult.ErrorMessage ?? "Analysis failed");
                }
                
                // Create CASA result from analysis
                var casaResult = CreateCASAResultFromAnalysis(analysisResult, examId, analyzedByUserId, settings);
                
                // Save to database
                using var db = new DatabaseService();
                
                // Check if result already exists
                var existingResult = await db.CASAResults
                    .FirstOrDefaultAsync(c => c.ExamId == examId);
                
                if (existingResult != null)
                {
                    // Update existing result
                    var oldResult = existingResult.Clone();
                    UpdateCASAResult(existingResult, casaResult);
                    
                    await db.SaveChangesAsync();
                    
                    // Log audit
                    await _auditService.LogTestResultAsync(
                        examId, "CASA", AuditAction.Update, analyzedByUserId, 
                        oldResult, existingResult, exam.PatientId);
                        
                    casaResult = existingResult;
                }
                else
                {
                    // Create new result
                    db.CASAResults.Add(casaResult);
                    await db.SaveChangesAsync();
                    
                    // Log audit
                    await _auditService.LogTestResultAsync(
                        examId, "CASA", AuditAction.Create, analyzedByUserId, 
                        null, casaResult, exam.PatientId);
                }
                
                // Update exam status
                await UpdateExamStatus(examId, analyzedByUserId);
                
                _logger.LogInformation($"CASA analysis completed for exam {examId}: {casaResult.TotalSpermCount} sperm detected");
                
                return casaResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CASA analysis failed for exam {examId}");
                
                await _auditService.LogCriticalOperationAsync(
                    "CASAResults", examId, AuditAction.Analyze, analyzedByUserId,
                    "CASA analysis failed", new { Error = ex.Message, VideoPath = videoPath });
                    
                throw;
            }
        }
        
        private CASAResult CreateCASAResultFromAnalysis(CASAAnalysisResult analysisResult, int examId, int analyzedByUserId, CASAAnalysisSettings settings)
        {
            var metrics = analysisResult.Metrics;
            
            return new CASAResult
            {
                ExamId = examId,
                
                // Basic Analysis Data
                TotalSpermCount = metrics.TotalSpermCount,
                Concentration = metrics.Concentration,
                Volume = 0.1, // Default volume, should be configurable
                TotalMotileSperm = metrics.TotalSpermCount * (metrics.TotalMotility / 100.0),
                
                // Motility Analysis
                ProgressiveMotility = metrics.ProgressiveMotility,
                NonProgressiveMotility = metrics.NonProgressiveMotility,
                ImmotileSperm = metrics.ImmotileSperm,
                TotalMotility = metrics.TotalMotility,
                
                // Velocity Parameters
                VCL = metrics.VCL,
                VSL = metrics.VSL,
                VAP = metrics.VAP,
                LIN = metrics.LIN,
                STR = metrics.STR,
                WOB = metrics.WOB,
                ALH = metrics.ALH,
                BCF = metrics.BCF,
                
                // Analysis Settings
                TemperatureC = settings.TemperatureC,
                AnalysisDurationSeconds = (int)analysisResult.AnalysisDuration.TotalSeconds,
                FramesAnalyzed = analysisResult.TotalFrames,
                FrameRate = analysisResult.FrameRate,
                
                // AI Analysis Results
                DetectedSpermCount = metrics.TotalSpermCount,
                TrackedSpermCount = metrics.TrackedSpermCount,
                AiConfidenceAverage = CalculateAverageConfidence(analysisResult.Tracks),
                YoloDetectionData = SerializeDetectionData(analysisResult.FrameDetections),
                DeepSortTrackingData = SerializeTrackingData(analysisResult.Tracks),
                
                // Video Analysis
                VideoFilePath = analysisResult.VideoPath,
                IsAiAnalysisCompleted = true,
                AiAnalysisCompletedAt = DateTime.UtcNow,
                
                // Calibration Data
                MicronPerPixel = settings.PixelsToMicronsRatio,
                MagnificationUsed = 400, // Default magnification
                
                // Analysis Metadata
                AnalysisDate = DateTime.UtcNow,
                AnalyzedByUserId = analyzedByUserId,
                SoftwareVersion = "1.0.0",
                
                // Interpretation
                Interpretation = DetermineInterpretation(metrics),
                InterpretationNotes = GenerateInterpretationNotes(metrics)
            };
        }
        
        private CASAInterpretation DetermineInterpretation(CASAMetrics metrics)
        {
            // WHO 2010 reference values
            var isOligospermia = metrics.Concentration < 15; // million/mL
            var isAsthenospermia = metrics.ProgressiveMotility < 32; // %
            var isTeratospermia = false; // Would need morphology analysis
            
            if (metrics.TotalSpermCount == 0)
                return CASAInterpretation.Azoospermia;
                
            if (isOligospermia && isAsthenospermia)
                return CASAInterpretation.OligoAsthenospermia;
            else if (isOligospermia)
                return CASAInterpretation.Oligospermia;
            else if (isAsthenospermia)
                return CASAInterpretation.Asthenospermia;
            else
                return CASAInterpretation.Normal;
        }
        
        private string GenerateInterpretationNotes(CASAMetrics metrics)
        {
            var notes = new List&lt;string&gt;();
            
            // Concentration assessment
            if (metrics.Concentration < 15)
                notes.Add("تركيز الحيوانات المنوية أقل من الطبيعي (أوليجوسبرميا)");
            else if (metrics.Concentration >= 15)
                notes.Add("تركيز الحيوانات المنوية طبيعي");
                
            // Motility assessment
            if (metrics.ProgressiveMotility < 32)
                notes.Add("حركة الحيوانات المنوية التقدمية أقل من الطبيعي");
            else
                notes.Add("حركة الحيوانات المنوية التقدمية طبيعية");
                
            if (metrics.TotalMotility < 40)
                notes.Add("إجمالي حركة الحيوانات المنوية أقل من الطبيعي");
            else
                notes.Add("إجمالي حركة الحيوانات المنوية طبيعية");
                
            // Velocity assessment
            if (metrics.VCL > 0)
            {
                if (metrics.VCL < 20)
                    notes.Add("سرعة الحيوانات المنوية منخفضة");
                else if (metrics.VCL > 50)
                    notes.Add("سرعة الحيوانات المنوية عالية");
                else
                    notes.Add("سرعة الحيوانات المنوية ضمن المعدل الطبيعي");
            }
            
            return string.Join(". ", notes) + ".";
        }
        
        private void UpdateCASAResult(CASAResult existing, CASAResult newResult)
        {
            existing.TotalSpermCount = newResult.TotalSpermCount;
            existing.Concentration = newResult.Concentration;
            existing.Volume = newResult.Volume;
            existing.TotalMotileSperm = newResult.TotalMotileSperm;
            existing.ProgressiveMotility = newResult.ProgressiveMotility;
            existing.NonProgressiveMotility = newResult.NonProgressiveMotility;
            existing.ImmotileSperm = newResult.ImmotileSperm;
            existing.TotalMotility = newResult.TotalMotility;
            existing.VCL = newResult.VCL;
            existing.VSL = newResult.VSL;
            existing.VAP = newResult.VAP;
            existing.LIN = newResult.LIN;
            existing.STR = newResult.STR;
            existing.WOB = newResult.WOB;
            existing.ALH = newResult.ALH;
            existing.BCF = newResult.BCF;
            existing.DetectedSpermCount = newResult.DetectedSpermCount;
            existing.TrackedSpermCount = newResult.TrackedSpermCount;
            existing.AiConfidenceAverage = newResult.AiConfidenceAverage;
            existing.YoloDetectionData = newResult.YoloDetectionData;
            existing.DeepSortTrackingData = newResult.DeepSortTrackingData;
            existing.IsAiAnalysisCompleted = newResult.IsAiAnalysisCompleted;
            existing.AiAnalysisCompletedAt = newResult.AiAnalysisCompletedAt;
            existing.AnalysisDate = newResult.AnalysisDate;
            existing.AnalyzedByUserId = newResult.AnalyzedByUserId;
            existing.Interpretation = newResult.Interpretation;
            existing.InterpretationNotes = newResult.InterpretationNotes;
        }
        
        private async Task&lt;Exam?&gt; GetExamAsync(int examId)
        {
            using var db = new DatabaseService();
            return await db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
        }
        
        private async Task UpdateExamStatus(int examId, int userId)
        {
            using var db = new DatabaseService();
            var exam = await db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
            
            if (exam != null && exam.Status == ExamStatus.InProgress)
            {
                exam.Status = ExamStatus.Completed;
                exam.CompletedDate = DateTime.UtcNow;
                await db.SaveChangesAsync();
                
                await _auditService.LogExamOperationAsync(
                    examId, AuditAction.Update, userId, null, exam, exam.PatientId);
            }
        }
        
        private double? CalculateAverageConfidence(List&lt;SpermTrack&gt; tracks)
        {
            var confidences = tracks.SelectMany(t => t.Confidences).ToList();
            return confidences.Count > 0 ? confidences.Average() : null;
        }
        
        private string SerializeDetectionData(List&lt;FrameDetection&gt; frameDetections)
        {
            try
            {
                var summary = new
                {
                    TotalFrames = frameDetections.Count,
                    TotalDetections = frameDetections.Sum(f => f.Detections.Count),
                    AverageDetectionsPerFrame = frameDetections.Count > 0 ? frameDetections.Average(f => f.Detections.Count) : 0,
                    DetectionsByFrame = frameDetections.Take(10).Select(f => new // Limit to first 10 frames for storage
                    {
                        FrameIndex = f.FrameIndex,
                        DetectionCount = f.Detections.Count,
                        Detections = f.Detections.Take(5).Select(d => new // Limit to top 5 detections per frame
                        {
                            CenterX = d.CenterX,
                            CenterY = d.CenterY,
                            Confidence = d.Confidence
                        })
                    })
                };
                return JsonSerializer.Serialize(summary);
            }
            catch
            {
                return "{}";
            }
        }
        
        private string SerializeTrackingData(List&lt;SpermTrack&gt; tracks)
        {
            try
            {
                var summary = new
                {
                    TotalTracks = tracks.Count,
                    ValidTracks = tracks.Count(t => t.IsValidForAnalysis()),
                    AverageTrackLength = tracks.Count > 0 ? tracks.Average(t => t.Positions.Count) : 0,
                    LongestTrack = tracks.Count > 0 ? tracks.Max(t => t.Positions.Count) : 0,
                    TrackSummary = tracks.Take(20).Select(t => new // Limit to first 20 tracks
                    {
                        TrackId = t.TrackId,
                        Length = t.Positions.Count,
                        TotalDistance = t.CalculateTotalDistance(),
                        StartFrame = t.StartFrame,
                        EndFrame = t.EndFrame
                    })
                };
                return JsonSerializer.Serialize(summary);
            }
            catch
            {
                return "{}";
            }
        }
        
        public async Task&lt;CASAResult?&gt; GetResultAsync(int examId)
        {
            try
            {
                using var db = new DatabaseService();
                return await db.CASAResults
                    .Include(c => c.Exam)
                    .ThenInclude(e => e.Patient)
                    .Include(c => c.AnalyzedByUser)
                    .FirstOrDefaultAsync(c => c.ExamId == examId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get CASA result for exam {examId}");
                return null;
            }
        }
        
        public async Task&lt;bool&gt; DeleteResultAsync(int examId, int userId)
        {
            try
            {
                using var db = new DatabaseService();
                var result = await db.CASAResults.FirstOrDefaultAsync(c => c.ExamId == examId);
                
                if (result != null)
                {
                    db.CASAResults.Remove(result);
                    await db.SaveChangesAsync();
                    
                    await _auditService.LogTestResultAsync&lt;CASAResult&gt;(
                        examId, "CASA", AuditAction.Delete, userId, result, null);
                        
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete CASA result for exam {examId}");
                return false;
            }
        }
    }
}