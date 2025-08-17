using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Text.Json;
using MedicalLabAnalyzer.Models;
using MathNet.Numerics.Statistics;

namespace MedicalLabAnalyzer.Services.AI
{
    public class CASAAnalysisService
    {
        private readonly ILogger&lt;CASAAnalysisService&gt; _logger;
        private readonly MediaService _mediaService;
        private InferenceSession? _yoloSession;
        private readonly string _modelPath;
        private readonly Dictionary&lt;int, SpermTrack&gt; _activeTracks;
        private int _nextTrackId = 1;
        
        // YOLO model configuration
        private const int ModelInputSize = 640;
        private const float ConfidenceThreshold = 0.5f;
        private const float NmsThreshold = 0.4f;
        
        // CASA analysis parameters
        private const double PixelsToMicrons = 0.2; // Default calibration, should be configurable
        private const double FramesPerSecond = 30.0; // Will be determined from video
        
        public CASAAnalysisService(ILogger&lt;CASAAnalysisService&gt; logger, MediaService mediaService)
        {
            _logger = logger;
            _mediaService = mediaService;
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI", "Models", "sperm_yolov8.onnx");
            _activeTracks = new Dictionary&lt;int, SpermTrack&gt;();
            
            InitializeYoloModel();
        }
        
        private void InitializeYoloModel()
        {
            try
            {
                if (File.Exists(_modelPath))
                {
                    var sessionOptions = new SessionOptions
                    {
                        EnableCpuMemArena = true,
                        EnableMemoryPattern = true,
                        GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_EXTENDED
                    };
                    
                    // Try to use GPU if available
                    try
                    {
                        sessionOptions.AppendExecutionProvider_CUDA(0);
                        _logger.LogInformation("CUDA GPU acceleration enabled for YOLO model");
                    }
                    catch
                    {
                        _logger.LogInformation("CUDA not available, using CPU for YOLO model");
                    }
                    
                    _yoloSession = new InferenceSession(_modelPath, sessionOptions);
                    _logger.LogInformation("YOLO model loaded successfully");
                }
                else
                {
                    _logger.LogWarning($"YOLO model not found at {_modelPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize YOLO model");
            }
        }
        
        public async Task&lt;CASAAnalysisResult&gt; AnalyzeVideoAsync(string videoPath, CASAAnalysisSettings settings)
        {
            var result = new CASAAnalysisResult
            {
                VideoPath = videoPath,
                AnalysisStartTime = DateTime.UtcNow,
                Settings = settings
            };
            
            try
            {
                if (_yoloSession == null)
                {
                    throw new InvalidOperationException("YOLO model not initialized");
                }
                
                // Get video information
                var videoInfo = await _mediaService.GetVideoInfoAsync(videoPath);
                if (videoInfo == null)
                {
                    throw new ArgumentException("Invalid video file");
                }
                
                result.VideoInfo = videoInfo;
                result.FrameRate = videoInfo.FrameRate;
                
                // Extract frames for analysis
                var frames = await _mediaService.ExtractVideoFramesAsync(
                    videoPath, 
                    settings.MaxFrames, 
                    settings.StartTime, 
                    settings.EndTime);
                
                if (frames.Count == 0)
                {
                    throw new ArgumentException("No frames extracted from video");
                }
                
                result.TotalFrames = frames.Count;
                
                // Analyze each frame
                var frameDetections = new List&lt;FrameDetection&gt;();
                
                for (int frameIndex = 0; frameIndex &lt; frames.Count; frameIndex++)
                {
                    var detections = await AnalyzeFrameAsync(frames[frameIndex], frameIndex);
                    frameDetections.Add(new FrameDetection
                    {
                        FrameIndex = frameIndex,
                        Timestamp = TimeSpan.FromSeconds(frameIndex / result.FrameRate),
                        Detections = detections
                    });
                    
                    // Update progress
                    result.Progress = (double)(frameIndex + 1) / frames.Count;
                    
                    _logger.LogDebug($"Frame {frameIndex + 1}/{frames.Count} analyzed: {detections.Count} detections");
                }
                
                result.FrameDetections = frameDetections;
                
                // Perform tracking using DeepSORT-like algorithm
                result.Tracks = PerformTracking(frameDetections);
                
                // Calculate CASA metrics
                result.Metrics = CalculateCASAMetrics(result.Tracks, settings);
                
                result.AnalysisEndTime = DateTime.UtcNow;
                result.AnalysisDuration = result.AnalysisEndTime - result.AnalysisStartTime;
                result.IsCompleted = true;
                
                _logger.LogInformation($"CASA analysis completed. Found {result.Tracks.Count} sperm tracks with {result.Metrics.TotalSpermCount} total detections");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CASA analysis failed for video {videoPath}");
                result.ErrorMessage = ex.Message;
                result.AnalysisEndTime = DateTime.UtcNow;
                return result;
            }
        }
        
        private async Task&lt;List&lt;SpermDetection&gt;&gt; AnalyzeFrameAsync(byte[] frameData, int frameIndex)
        {
            var detections = new List&lt;SpermDetection&gt;();
            
            try
            {
                // Convert frame to OpenCV Mat
                using var frameMat = Mat.FromImageData(frameData);
                
                // Preprocess frame for YOLO
                var inputTensor = PreprocessFrame(frameMat);
                
                // Run YOLO inference
                var inputs = new Dictionary&lt;string, OrtValue&gt;
                {
                    { "images", inputTensor }
                };
                
                using var results = _yoloSession!.Run(inputs);
                var output = results[0].AsTensor&lt;float&gt;();
                
                // Post-process YOLO output
                detections = PostprocessYoloOutput(output, frameMat.Width, frameMat.Height, frameIndex);
                
                inputTensor.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to analyze frame {frameIndex}");
            }
            
            return detections;
        }
        
        private OrtValue PreprocessFrame(Mat frame)
        {
            // Resize frame to model input size
            using var resized = new Mat();
            Cv2.Resize(frame, resized, new Size(ModelInputSize, ModelInputSize));
            
            // Convert BGR to RGB
            using var rgb = new Mat();
            Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);
            
            // Normalize pixel values to [0, 1]
            var tensorData = new float[1 * 3 * ModelInputSize * ModelInputSize];
            var pixelData = new byte[rgb.Width * rgb.Height * rgb.Channels()];
            rgb.GetArray(out pixelData);
            
            for (int i = 0; i &lt; pixelData.Length; i += 3)
            {
                var pixelIndex = i / 3;
                var r = pixelData[i] / 255.0f;
                var g = pixelData[i + 1] / 255.0f;
                var b = pixelData[i + 2] / 255.0f;
                
                // CHW format
                tensorData[pixelIndex] = r;
                tensorData[ModelInputSize * ModelInputSize + pixelIndex] = g;
                tensorData[2 * ModelInputSize * ModelInputSize + pixelIndex] = b;
            }
            
            var tensor = new DenseTensor&lt;float&gt;(tensorData, new[] { 1, 3, ModelInputSize, ModelInputSize });
            return OrtValue.CreateTensorValueFromMemory(tensor.Buffer, new[] { 1L, 3L, ModelInputSize, ModelInputSize });
        }
        
        private List&lt;SpermDetection&gt; PostprocessYoloOutput(Tensor&lt;float&gt; output, int originalWidth, int originalHeight, int frameIndex)
        {
            var detections = new List&lt;SpermDetection&gt;();
            
            try
            {
                var outputData = output.ToArray();
                var numDetections = output.Dimensions[1];
                var numClasses = output.Dimensions[2] - 5; // x, y, w, h, conf + classes
                
                var validDetections = new List&lt;(float x, float y, float w, float h, float conf, int classId)&gt;();
                
                // Extract valid detections
                for (int i = 0; i &lt; numDetections; i++)
                {
                    var confidence = outputData[i * (5 + numClasses) + 4];
                    
                    if (confidence &gt; ConfidenceThreshold)
                    {
                        var x = outputData[i * (5 + numClasses) + 0];
                        var y = outputData[i * (5 + numClasses) + 1];
                        var w = outputData[i * (5 + numClasses) + 2];
                        var h = outputData[i * (5 + numClasses) + 3];
                        
                        // Find class with highest probability
                        var maxClassScore = 0f;
                        var classId = 0;
                        for (int c = 0; c &lt; numClasses; c++)
                        {
                            var classScore = outputData[i * (5 + numClasses) + 5 + c];
                            if (classScore &gt; maxClassScore)
                            {
                                maxClassScore = classScore;
                                classId = c;
                            }
                        }
                        
                        var finalConfidence = confidence * maxClassScore;
                        if (finalConfidence &gt; ConfidenceThreshold)
                        {
                            validDetections.Add((x, y, w, h, finalConfidence, classId));
                        }
                    }
                }
                
                // Apply Non-Maximum Suppression
                var nmsDetections = ApplyNMS(validDetections, NmsThreshold);
                
                // Convert to SpermDetection objects
                foreach (var detection in nmsDetections)
                {
                    // Scale coordinates back to original image size
                    var scaleX = (float)originalWidth / ModelInputSize;
                    var scaleY = (float)originalHeight / ModelInputSize;
                    
                    var centerX = detection.x * scaleX;
                    var centerY = detection.y * scaleY;
                    var width = detection.w * scaleX;
                    var height = detection.h * scaleY;
                    
                    detections.Add(new SpermDetection
                    {
                        FrameIndex = frameIndex,
                        BoundingBox = new RectangleF(
                            centerX - width / 2,
                            centerY - height / 2,
                            width,
                            height),
                        Confidence = detection.conf,
                        ClassId = detection.classId,
                        CenterX = centerX,
                        CenterY = centerY
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post-process YOLO output");
            }
            
            return detections;
        }
        
        private List&lt;(float x, float y, float w, float h, float conf, int classId)&gt; ApplyNMS(
            List&lt;(float x, float y, float w, float h, float conf, int classId)&gt; detections, 
            float nmsThreshold)
        {
            var result = new List&lt;(float x, float y, float w, float h, float conf, int classId)&gt;();
            var sortedDetections = detections.OrderByDescending(d =&gt; d.conf).ToList();
            
            while (sortedDetections.Count &gt; 0)
            {
                var best = sortedDetections[0];
                result.Add(best);
                sortedDetections.RemoveAt(0);
                
                // Remove overlapping detections
                for (int i = sortedDetections.Count - 1; i &gt;= 0; i--)
                {
                    var current = sortedDetections[i];
                    var iou = CalculateIoU(best, current);
                    
                    if (iou &gt; nmsThreshold)
                    {
                        sortedDetections.RemoveAt(i);
                    }
                }
            }
            
            return result;
        }
        
        private float CalculateIoU((float x, float y, float w, float h, float conf, int classId) box1, 
                                   (float x, float y, float w, float h, float conf, int classId) box2)
        {
            var x1 = Math.Max(box1.x - box1.w / 2, box2.x - box2.w / 2);
            var y1 = Math.Max(box1.y - box1.h / 2, box2.y - box2.h / 2);
            var x2 = Math.Min(box1.x + box1.w / 2, box2.x + box2.w / 2);
            var y2 = Math.Min(box1.y + box1.h / 2, box2.y + box2.h / 2);
            
            var intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            var box1Area = box1.w * box1.h;
            var box2Area = box2.w * box2.h;
            var unionArea = box1Area + box2Area - intersectionArea;
            
            return unionArea &gt; 0 ? (float)(intersectionArea / unionArea) : 0;
        }
        
        private List&lt;SpermTrack&gt; PerformTracking(List&lt;FrameDetection&gt; frameDetections)
        {
            var tracks = new Dictionary&lt;int, SpermTrack&gt;();
            var maxTrackDistance = 50.0; // Maximum distance for track association
            var maxFramesWithoutDetection = 5;
            
            foreach (var frameDetection in frameDetections)
            {
                var unassignedDetections = frameDetection.Detections.ToList();
                var activeTracks = tracks.Values.Where(t =&gt; !t.IsCompleted).ToList();
                
                // Associate detections with existing tracks
                foreach (var track in activeTracks)
                {
                    var lastPosition = track.Positions.LastOrDefault();
                    if (lastPosition == null) continue;
                    
                    var closestDetection = unassignedDetections
                        .OrderBy(d =&gt; Math.Sqrt(Math.Pow(d.CenterX - lastPosition.X, 2) + Math.Pow(d.CenterY - lastPosition.Y, 2)))
                        .FirstOrDefault();
                    
                    if (closestDetection != null)
                    {
                        var distance = Math.Sqrt(Math.Pow(closestDetection.CenterX - lastPosition.X, 2) + 
                                                Math.Pow(closestDetection.CenterY - lastPosition.Y, 2));
                        
                        if (distance &lt; maxTrackDistance)
                        {
                            track.AddDetection(closestDetection);
                            unassignedDetections.Remove(closestDetection);
                        }
                        else
                        {
                            track.FramesWithoutDetection++;
                        }
                    }
                    else
                    {
                        track.FramesWithoutDetection++;
                    }
                    
                    // Mark track as completed if no detection for too long
                    if (track.FramesWithoutDetection &gt; maxFramesWithoutDetection)
                    {
                        track.IsCompleted = true;
                        track.EndFrame = frameDetection.FrameIndex;
                    }
                }
                
                // Create new tracks for unassigned detections
                foreach (var detection in unassignedDetections)
                {
                    var newTrack = new SpermTrack
                    {
                        TrackId = _nextTrackId++,
                        StartFrame = frameDetection.FrameIndex,
                        FrameRate = FramesPerSecond
                    };
                    newTrack.AddDetection(detection);
                    tracks[newTrack.TrackId] = newTrack;
                }
            }
            
            // Complete all remaining tracks
            foreach (var track in tracks.Values.Where(t =&gt; !t.IsCompleted))
            {
                track.IsCompleted = true;
                track.EndFrame = frameDetections.LastOrDefault()?.FrameIndex ?? 0;
            }
            
            return tracks.Values.Where(t =&gt; t.Positions.Count &gt;= 3).ToList(); // Minimum 3 positions for valid track
        }
        
        private CASAMetrics CalculateCASAMetrics(List&lt;SpermTrack&gt; tracks, CASAAnalysisSettings settings)
        {
            var metrics = new CASAMetrics();
            
            try
            {
                var validTracks = tracks.Where(t =&gt; t.IsValidForAnalysis()).ToList();
                
                metrics.TotalSpermCount = validTracks.Count;
                metrics.TrackedSpermCount = validTracks.Count(t =&gt; t.Positions.Count &gt;= 5);
                
                if (validTracks.Count == 0)
                {
                    return metrics;
                }
                
                var velocities = new List&lt;double&gt;();
                var straightLineVelocities = new List&lt;double&gt;();
                var linearities = new List&lt;double&gt;();
                var motileCount = 0;
                
                foreach (var track in validTracks)
                {
                    var trackMetrics = track.CalculateMotilityMetrics(PixelsToMicrons);
                    
                    if (trackMetrics.VCL &gt; 0)
                    {
                        velocities.Add(trackMetrics.VCL);
                        straightLineVelocities.Add(trackMetrics.VSL);
                        linearities.Add(trackMetrics.LIN);
                        
                        if (trackMetrics.VCL &gt; settings.MotilityThreshold)
                        {
                            motileCount++;
                        }
                    }
                }
                
                // Calculate average metrics
                if (velocities.Count &gt; 0)
                {
                    metrics.VCL = velocities.Average();
                    metrics.VSL = straightLineVelocities.Average();
                    metrics.LIN = linearities.Average();
                    metrics.VAP = velocities.Select((v, i) =&gt; (v + straightLineVelocities[i]) / 2).Average();
                    
                    // Calculate additional derived parameters
                    metrics.STR = metrics.VAP &gt; 0 ? metrics.VSL / metrics.VAP : 0;
                    metrics.WOB = metrics.VCL &gt; 0 ? metrics.VAP / metrics.VCL : 0;
                    metrics.ALH = validTracks.Average(t =&gt; t.CalculateALH(PixelsToMicrons));
                    metrics.BCF = validTracks.Average(t =&gt; t.CalculateBCF());
                }
                
                // Calculate motility percentages
                metrics.TotalMotility = validTracks.Count &gt; 0 ? (double)motileCount / validTracks.Count * 100 : 0;
                metrics.ProgressiveMotility = CalculateProgressiveMotility(validTracks, settings);
                metrics.NonProgressiveMotility = metrics.TotalMotility - metrics.ProgressiveMotility;
                metrics.ImmotileSperm = 100 - metrics.TotalMotility;
                
                // Concentration estimation (simplified)
                var analysisArea = settings.AnalysisAreaMm2 ?? 1.0; // Default 1 mm²
                var depth = settings.ChamberDepthMicrons ?? 20.0; // Default 20 μm
                var volumeAnalyzed = analysisArea * depth / 1000; // μL
                
                metrics.Concentration = metrics.TotalSpermCount / volumeAnalyzed;
                
                _logger.LogInformation($"CASA metrics calculated: {metrics.TotalSpermCount} sperm, {metrics.TotalMotility:F1}% motile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate CASA metrics");
            }
            
            return metrics;
        }
        
        private double CalculateProgressiveMotility(List&lt;SpermTrack&gt; tracks, CASAAnalysisSettings settings)
        {
            var progressiveCount = 0;
            var totalCount = tracks.Count;
            
            foreach (var track in tracks)
            {
                var metrics = track.CalculateMotilityMetrics(PixelsToMicrons);
                
                // Progressive motility criteria: VCL > threshold AND LIN > 0.45
                if (metrics.VCL &gt; settings.MotilityThreshold && metrics.LIN &gt; 0.45)
                {
                    progressiveCount++;
                }
            }
            
            return totalCount &gt; 0 ? (double)progressiveCount / totalCount * 100 : 0;
        }
        
        public void Dispose()
        {
            _yoloSession?.Dispose();
        }
    }
    
    // Supporting classes for CASA analysis
    public class CASAAnalysisSettings
    {
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public int MaxFrames { get; set; } = 100;
        public double MotilityThreshold { get; set; } = 5.0; // μm/s
        public double PixelsToMicronsRatio { get; set; } = 0.2;
        public double? AnalysisAreaMm2 { get; set; }
        public double? ChamberDepthMicrons { get; set; }
        public double TemperatureC { get; set; } = 37.0;
    }
    
    public class CASAAnalysisResult
    {
        public string VideoPath { get; set; } = string.Empty;
        public DateTime AnalysisStartTime { get; set; }
        public DateTime AnalysisEndTime { get; set; }
        public TimeSpan AnalysisDuration { get; set; }
        public bool IsCompleted { get; set; }
        public string? ErrorMessage { get; set; }
        public double Progress { get; set; }
        
        public VideoInfo? VideoInfo { get; set; }
        public double FrameRate { get; set; }
        public int TotalFrames { get; set; }
        public CASAAnalysisSettings Settings { get; set; } = new();
        
        public List&lt;FrameDetection&gt; FrameDetections { get; set; } = new();
        public List&lt;SpermTrack&gt; Tracks { get; set; } = new();
        public CASAMetrics Metrics { get; set; } = new();
    }
    
    public class FrameDetection
    {
        public int FrameIndex { get; set; }
        public TimeSpan Timestamp { get; set; }
        public List&lt;SpermDetection&gt; Detections { get; set; } = new();
    }
    
    public class SpermDetection
    {
        public int FrameIndex { get; set; }
        public RectangleF BoundingBox { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
    }
    
    public class SpermTrack
    {
        public int TrackId { get; set; }
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
        public double FrameRate { get; set; }
        public bool IsCompleted { get; set; }
        public int FramesWithoutDetection { get; set; }
        
        public List&lt;PointF&gt; Positions { get; set; } = new();
        public List&lt;TimeSpan&gt; Timestamps { get; set; } = new();
        public List&lt;float&gt; Confidences { get; set; } = new();
        
        public void AddDetection(SpermDetection detection)
        {
            Positions.Add(new PointF(detection.CenterX, detection.CenterY));
            Timestamps.Add(TimeSpan.FromSeconds(detection.FrameIndex / FrameRate));
            Confidences.Add(detection.Confidence);
            FramesWithoutDetection = 0;
        }
        
        public bool IsValidForAnalysis()
        {
            return Positions.Count &gt;= 3 && CalculateTotalDistance() &gt; 10; // Minimum movement
        }
        
        public double CalculateTotalDistance()
        {
            double totalDistance = 0;
            for (int i = 1; i &lt; Positions.Count; i++)
            {
                var dx = Positions[i].X - Positions[i - 1].X;
                var dy = Positions[i].Y - Positions[i - 1].Y;
                totalDistance += Math.Sqrt(dx * dx + dy * dy);
            }
            return totalDistance;
        }
        
        public (double VCL, double VSL, double LIN, double VAP) CalculateMotilityMetrics(double pixelsToMicrons)
        {
            if (Positions.Count &lt; 2) return (0, 0, 0, 0);
            
            // Calculate Curvilinear Velocity (VCL) - average velocity along the actual path
            var totalDistance = CalculateTotalDistance() * pixelsToMicrons;
            var totalTime = (Timestamps.Last() - Timestamps.First()).TotalSeconds;
            var vcl = totalTime &gt; 0 ? totalDistance / totalTime : 0;
            
            // Calculate Straight Line Velocity (VSL) - velocity from start to end
            var startPos = Positions.First();
            var endPos = Positions.Last();
            var straightDistance = Math.Sqrt(Math.Pow(endPos.X - startPos.X, 2) + Math.Pow(endPos.Y - startPos.Y, 2)) * pixelsToMicrons;
            var vsl = totalTime &gt; 0 ? straightDistance / totalTime : 0;
            
            // Calculate Average Path Velocity (VAP) - velocity along the smoothed path
            var smoothedPositions = SmoothPath(Positions);
            var smoothedDistance = CalculatePathDistance(smoothedPositions) * pixelsToMicrons;
            var vap = totalTime &gt; 0 ? smoothedDistance / totalTime : 0;
            
            // Calculate Linearity (LIN) = VSL/VCL
            var lin = vcl &gt; 0 ? vsl / vcl : 0;
            
            return (vcl, vsl, lin, vap);
        }
        
        public double CalculateALH(double pixelsToMicrons)
        {
            // Amplitude of Lateral Head displacement - average deviation from the smooth path
            if (Positions.Count &lt; 3) return 0;
            
            var smoothedPositions = SmoothPath(Positions);
            var deviations = new List&lt;double&gt;();
            
            for (int i = 0; i &lt; Positions.Count && i &lt; smoothedPositions.Count; i++)
            {
                var deviation = Math.Sqrt(
                    Math.Pow(Positions[i].X - smoothedPositions[i].X, 2) + 
                    Math.Pow(Positions[i].Y - smoothedPositions[i].Y, 2)) * pixelsToMicrons;
                deviations.Add(deviation);
            }
            
            return deviations.Count &gt; 0 ? deviations.Average() : 0;
        }
        
        public double CalculateBCF()
        {
            // Beat Cross Frequency - frequency of head crossing the smooth path
            if (Positions.Count &lt; 5) return 0;
            
            var smoothedPositions = SmoothPath(Positions);
            var crossings = 0;
            var lastSide = 0; // -1 for left, 1 for right, 0 for on path
            
            for (int i = 1; i &lt; Positions.Count - 1 && i &lt; smoothedPositions.Count - 1; i++)
            {
                // Calculate which side of the smooth path the current position is on
                var currentSide = CalculatePathSide(Positions[i], smoothedPositions[i], smoothedPositions[i + 1]);
                
                if (lastSide != 0 && currentSide != 0 && lastSide != currentSide)
                {
                    crossings++;
                }
                
                lastSide = currentSide;
            }
            
            var totalTime = (Timestamps.Last() - Timestamps.First()).TotalSeconds;
            return totalTime &gt; 0 ? crossings / totalTime : 0;
        }
        
        private List&lt;PointF&gt; SmoothPath(List&lt;PointF&gt; positions)
        {
            if (positions.Count &lt; 3) return positions.ToList();
            
            var smoothed = new List&lt;PointF&gt; { positions[0] };
            
            for (int i = 1; i &lt; positions.Count - 1; i++)
            {
                var smoothX = (positions[i - 1].X + positions[i].X + positions[i + 1].X) / 3;
                var smoothY = (positions[i - 1].Y + positions[i].Y + positions[i + 1].Y) / 3;
                smoothed.Add(new PointF(smoothX, smoothY));
            }
            
            smoothed.Add(positions[positions.Count - 1]);
            return smoothed;
        }
        
        private double CalculatePathDistance(List&lt;PointF&gt; positions)
        {
            double distance = 0;
            for (int i = 1; i &lt; positions.Count; i++)
            {
                var dx = positions[i].X - positions[i - 1].X;
                var dy = positions[i].Y - positions[i - 1].Y;
                distance += Math.Sqrt(dx * dx + dy * dy);
            }
            return distance;
        }
        
        private int CalculatePathSide(PointF current, PointF pathStart, PointF pathEnd)
        {
            // Calculate which side of the line segment the point is on
            var cross = (pathEnd.X - pathStart.X) * (current.Y - pathStart.Y) - (pathEnd.Y - pathStart.Y) * (current.X - pathStart.X);
            return cross > 1 ? 1 : cross &lt; -1 ? -1 : 0;
        }
    }
    
    public class CASAMetrics
    {
        public int TotalSpermCount { get; set; }
        public int TrackedSpermCount { get; set; }
        public double Concentration { get; set; } // million/mL
        
        // Motility parameters
        public double TotalMotility { get; set; } // %
        public double ProgressiveMotility { get; set; } // %
        public double NonProgressiveMotility { get; set; } // %
        public double ImmotileSperm { get; set; } // %
        
        // Velocity parameters (μm/s)
        public double VCL { get; set; } // Curvilinear Velocity
        public double VSL { get; set; } // Straight Line Velocity
        public double VAP { get; set; } // Average Path Velocity
        
        // Derived parameters
        public double LIN { get; set; } // Linearity = VSL/VCL
        public double STR { get; set; } // Straightness = VSL/VAP
        public double WOB { get; set; } // Wobble = VAP/VCL
        public double ALH { get; set; } // Amplitude of Lateral Head displacement
        public double BCF { get; set; } // Beat Cross Frequency
    }
}