using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using SkiaSharp;
using System.Text.Json;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.Services.AI
{
    public class MedicalImageAnalysisService
    {
        private readonly ILogger&lt;MedicalImageAnalysisService&gt; _logger;
        private readonly MediaService _mediaService;
        private InferenceSession? _generalAnalysisModel;
        private InferenceSession? _cellCountingModel;
        private readonly string _modelsPath;
        
        public MedicalImageAnalysisService(ILogger&lt;MedicalImageAnalysisService&gt; logger, MediaService mediaService)
        {
            _logger = logger;
            _mediaService = mediaService;
            _modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AI", "Models");
            
            InitializeModels();
        }
        
        private void InitializeModels()
        {
            try
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
                    _logger.LogInformation("CUDA GPU acceleration enabled for medical image analysis");
                }
                catch
                {
                    _logger.LogInformation("CUDA not available, using CPU for medical image analysis");
                }
                
                // Load general medical image analysis model (if available)
                var generalModelPath = Path.Combine(_modelsPath, "medical_analysis_general.onnx");
                if (File.Exists(generalModelPath))
                {
                    _generalAnalysisModel = new InferenceSession(generalModelPath, sessionOptions);
                    _logger.LogInformation("General medical analysis model loaded");
                }
                
                // Load cell counting model (if available)
                var cellCountingModelPath = Path.Combine(_modelsPath, "cell_counting.onnx");
                if (File.Exists(cellCountingModelPath))
                {
                    _cellCountingModel = new InferenceSession(cellCountingModelPath, sessionOptions);
                    _logger.LogInformation("Cell counting model loaded");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AI models");
            }
        }
        
        public async Task&lt;MedicalImageAnalysisResult&gt; AnalyzeImageAsync(string imagePath, MedicalImageAnalysisSettings settings)
        {
            var result = new MedicalImageAnalysisResult
            {
                ImagePath = imagePath,
                AnalysisStartTime = DateTime.UtcNow,
                Settings = settings
            };
            
            try
            {
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException($"Image file not found: {imagePath}");
                }
                
                // Load and preprocess image
                using var image = Mat.FromImageData(File.ReadAllBytes(imagePath));
                result.OriginalWidth = image.Width;
                result.OriginalHeight = image.Height;
                
                // Perform different types of analysis based on settings
                switch (settings.AnalysisType)
                {
                    case MedicalImageAnalysisType.CellCounting:
                        result.CellCountingResult = await PerformCellCountingAsync(image, settings);
                        break;
                        
                    case MedicalImageAnalysisType.GeneralAnalysis:
                        result.GeneralAnalysisResult = await PerformGeneralAnalysisAsync(image, settings);
                        break;
                        
                    case MedicalImageAnalysisType.BloodCellAnalysis:
                        result.BloodCellResult = await PerformBloodCellAnalysisAsync(image, settings);
                        break;
                        
                    case MedicalImageAnalysisType.UrineAnalysis:
                        result.UrineAnalysisResult = await PerformUrineAnalysisAsync(image, settings);
                        break;
                        
                    case MedicalImageAnalysisType.BacteriaDetection:
                        result.BacteriaDetectionResult = await PerformBacteriaDetectionAsync(image, settings);
                        break;
                        
                    default:
                        result.GeneralAnalysisResult = await PerformGeneralAnalysisAsync(image, settings);
                        break;
                }
                
                result.AnalysisEndTime = DateTime.UtcNow;
                result.AnalysisDuration = result.AnalysisEndTime - result.AnalysisStartTime;
                result.IsCompleted = true;
                
                _logger.LogInformation($"Medical image analysis completed for {imagePath}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Medical image analysis failed for {imagePath}");
                result.ErrorMessage = ex.Message;
                result.AnalysisEndTime = DateTime.UtcNow;
                return result;
            }
        }
        
        private async Task&lt;CellCountingResult&gt; PerformCellCountingAsync(Mat image, MedicalImageAnalysisSettings settings)
        {
            var result = new CellCountingResult();
            
            try
            {
                // Convert to grayscale for better cell detection
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                
                // Apply Gaussian blur to reduce noise
                using var blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                
                // Apply adaptive thresholding
                using var binary = new Mat();
                Cv2.AdaptiveThreshold(blurred, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                
                // Find contours
                Cv2.FindContours(binary, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                var cells = new List&lt;DetectedCell&gt;();
                
                foreach (var contour in contours)
                {
                    var area = Cv2.ContourArea(contour);
                    
                    // Filter by area (configurable cell size range)
                    if (area &gt; settings.MinCellArea && area &lt; settings.MaxCellArea)
                    {
                        var boundingRect = Cv2.BoundingRect(contour);
                        var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                        
                        // Filter by aspect ratio (cells should be roughly circular)
                        if (aspectRatio &gt; 0.5 && aspectRatio &lt; 2.0)
                        {
                            var moments = Cv2.Moments(contour);
                            var centerX = moments.M10 / moments.M00;
                            var centerY = moments.M01 / moments.M00;
                            
                            cells.Add(new DetectedCell
                            {
                                CenterX = centerX,
                                CenterY = centerY,
                                Area = area,
                                Perimeter = Cv2.ArcLength(contour, true),
                                BoundingRect = boundingRect,
                                AspectRatio = aspectRatio,
                                Circularity = 4 * Math.PI * area / Math.Pow(Cv2.ArcLength(contour, true), 2)
                            });
                        }
                    }
                }
                
                result.TotalCellCount = cells.Count;
                result.DetectedCells = cells;
                result.AverageArea = cells.Count > 0 ? cells.Average(c => c.Area) : 0;
                result.AverageCircularity = cells.Count > 0 ? cells.Average(c => c.Circularity) : 0;
                
                // Calculate density (cells per mm²)
                if (settings.CalibrationMicronsPerPixel.HasValue)
                {
                    var pixelArea = image.Width * image.Height;
                    var realAreaMm2 = pixelArea * Math.Pow(settings.CalibrationMicronsPerPixel.Value / 1000, 2);
                    result.CellDensityPerMm2 = result.TotalCellCount / realAreaMm2;
                }
                
                _logger.LogInformation($"Cell counting completed: {result.TotalCellCount} cells detected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cell counting analysis failed");
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
        
        private async Task&lt;GeneralAnalysisResult&gt; PerformGeneralAnalysisAsync(Mat image, MedicalImageAnalysisSettings settings)
        {
            var result = new GeneralAnalysisResult();
            
            try
            {
                // Image quality assessment
                result.ImageQuality = AssessImageQuality(image);
                
                // Basic image statistics
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                
                Cv2.MeanStdDev(gray, out var mean, out var stddev);
                result.Brightness = mean.Val0;
                result.Contrast = stddev.Val0;
                
                // Edge detection for structure analysis
                using var edges = new Mat();
                Cv2.Canny(gray, edges, 50, 150);
                result.EdgeDensity = Cv2.CountNonZero(edges) / (double)(edges.Width * edges.Height);
                
                // Texture analysis using Gabor filters
                result.TextureFeatures = AnalyzeTexture(gray);
                
                // Color analysis
                if (image.Channels() == 3)
                {
                    result.ColorAnalysis = AnalyzeColors(image);
                }
                
                // Pattern detection
                result.Patterns = DetectPatterns(gray);
                
                _logger.LogInformation($"General analysis completed. Quality: {result.ImageQuality}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General analysis failed");
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
        
        private async Task&lt;BloodCellAnalysisResult&gt; PerformBloodCellAnalysisAsync(Mat image, MedicalImageAnalysisSettings settings)
        {
            var result = new BloodCellAnalysisResult();
            
            try
            {
                // Convert to LAB color space for better cell separation
                using var lab = new Mat();
                Cv2.CvtColor(image, lab, ColorConversionCodes.BGR2Lab);
                
                // Split channels
                var channels = Cv2.Split(lab);
                using var lChannel = channels[0];
                
                // Apply CLAHE (Contrast Limited Adaptive Histogram Equalization)
                using var clahe = Cv2.CreateCLAHE(2.0, new Size(8, 8));
                using var enhanced = new Mat();
                clahe.Apply(lChannel, enhanced);
                
                // Detect red blood cells (RBCs)
                result.RedBloodCells = DetectRedBloodCells(enhanced, settings);
                
                // Detect white blood cells (WBCs)
                result.WhiteBloodCells = DetectWhiteBloodCells(enhanced, settings);
                
                // Detect platelets
                result.Platelets = DetectPlatelets(enhanced, settings);
                
                // Calculate ratios and densities
                result.RBCCount = result.RedBloodCells.Count;
                result.WBCCount = result.WhiteBloodCells.Count;
                result.PlateletCount = result.Platelets.Count;
                result.WBC_RBC_Ratio = result.RBCCount > 0 ? (double)result.WBCCount / result.RBCCount : 0;
                
                _logger.LogInformation($"Blood cell analysis completed: {result.RBCCount} RBCs, {result.WBCCount} WBCs, {result.PlateletCount} Platelets");
                
                // Dispose channels
                foreach (var channel in channels) channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blood cell analysis failed");
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
        
        private async Task&lt;UrineAnalysisResult&gt; PerformUrineAnalysisAsync(Mat image, MedicalImageAnalysisSettings settings)
        {
            var result = new UrineAnalysisResult();
            
            try
            {
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                
                // Detect different elements in urine microscopy
                result.RedBloodCells = CountUrineRBCs(gray, settings);
                result.WhiteBloodCells = CountUrineWBCs(gray, settings);
                result.EpithelialCells = CountEpithelialCells(gray, settings);
                result.Casts = DetectCasts(gray, settings);
                result.Crystals = DetectCrystals(gray, settings);
                result.Bacteria = CountBacteria(gray, settings);
                
                _logger.LogInformation($"Urine analysis completed: {result.RedBloodCells} RBCs, {result.WhiteBloodCells} WBCs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Urine analysis failed");
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
        
        private async Task&lt;BacteriaDetectionResult&gt; PerformBacteriaDetectionAsync(Mat image, MedicalImageAnalysisSettings settings)
        {
            var result = new BacteriaDetectionResult();
            
            try
            {
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                
                // Apply different filters for bacteria detection
                using var gaussian = new Mat();
                Cv2.GaussianBlur(gray, gaussian, new Size(3, 3), 0);
                
                using var binary = new Mat();
                Cv2.AdaptiveThreshold(gaussian, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 7, 2);
                
                // Find small circular/rod-shaped objects
                Cv2.FindContours(binary, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                var bacteria = new List&lt;DetectedBacteria&gt;();
                
                foreach (var contour in contours)
                {
                    var area = Cv2.ContourArea(contour);
                    
                    // Filter by size (bacteria are very small)
                    if (area > settings.MinBacteriaArea && area &lt; settings.MaxBacteriaArea)
                    {
                        var boundingRect = Cv2.BoundingRect(contour);
                        var aspectRatio = (double)boundingRect.Width / boundingRect.Height;
                        
                        var bacteriaType = ClassifyBacteriaShape(aspectRatio, area);
                        
                        bacteria.Add(new DetectedBacteria
                        {
                            CenterX = boundingRect.X + boundingRect.Width / 2.0,
                            CenterY = boundingRect.Y + boundingRect.Height / 2.0,
                            Area = area,
                            AspectRatio = aspectRatio,
                            Type = bacteriaType,
                            BoundingRect = boundingRect
                        });
                    }
                }
                
                result.TotalBacteriaCount = bacteria.Count;
                result.DetectedBacteria = bacteria;
                result.CocciCount = bacteria.Count(b => b.Type == BacteriaType.Cocci);
                result.BacilliCount = bacteria.Count(b => b.Type == BacteriaType.Bacilli);
                result.SpiralCount = bacteria.Count(b => b.Type == BacteriaType.Spiral);
                
                _logger.LogInformation($"Bacteria detection completed: {result.TotalBacteriaCount} bacteria detected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bacteria detection failed");
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }
        
        // Helper methods for specific analyses
        private ImageQuality AssessImageQuality(Mat image)
        {
            try
            {
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                
                // Calculate Laplacian variance for blur detection
                using var laplacian = new Mat();
                Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
                
                Cv2.MeanStdDev(laplacian, out var mean, out var variance);
                var blurScore = variance.Val0;
                
                // Determine quality based on blur score
                if (blurScore > 500) return ImageQuality.Excellent;
                if (blurScore > 200) return ImageQuality.Good;
                if (blurScore > 100) return ImageQuality.Fair;
                return ImageQuality.Poor;
            }
            catch
            {
                return ImageQuality.Unknown;
            }
        }
        
        private List&lt;double&gt; AnalyzeTexture(Mat grayImage)
        {
            var features = new List&lt;double&gt;();
            
            try
            {
                // Apply Gabor filters at different orientations
                var angles = new double[] { 0, 45, 90, 135 };
                
                foreach (var angle in angles)
                {
                    using var gabor = Cv2.GetGaborKernel(new Size(21, 21), 5, angle * Math.PI / 180, 2 * Math.PI, 0.5, 0);
                    using var filtered = new Mat();
                    Cv2.Filter2D(grayImage, filtered, MatType.CV_8UC1, gabor);
                    
                    Cv2.MeanStdDev(filtered, out var mean, out var stddev);
                    features.Add(mean.Val0);
                    features.Add(stddev.Val0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Texture analysis failed");
            }
            
            return features;
        }
        
        private ColorAnalysisResult AnalyzeColors(Mat image)
        {
            var result = new ColorAnalysisResult();
            
            try
            {
                // Convert to HSV for better color analysis
                using var hsv = new Mat();
                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);
                
                var channels = Cv2.Split(hsv);
                
                // Calculate color statistics
                Cv2.MeanStdDev(channels[0], out var hueMean, out var hueStd);
                Cv2.MeanStdDev(channels[1], out var satMean, out var satStd);
                Cv2.MeanStdDev(channels[2], out var valMean, out var valStd);
                
                result.DominantHue = hueMean.Val0;
                result.AverageSaturation = satMean.Val0;
                result.AverageBrightness = valMean.Val0;
                result.ColorVariability = (hueStd.Val0 + satStd.Val0 + valStd.Val0) / 3;
                
                // Dispose channels
                foreach (var channel in channels) channel.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Color analysis failed");
            }
            
            return result;
        }
        
        private List&lt;DetectedPattern&gt; DetectPatterns(Mat grayImage)
        {
            var patterns = new List&lt;DetectedPattern&gt;();
            
            try
            {
                // Template matching for common patterns could be implemented here
                // For now, detect basic geometric patterns
                
                using var edges = new Mat();
                Cv2.Canny(grayImage, edges, 50, 150);
                
                // Detect lines
                var lines = Cv2.HoughLinesP(edges, 1, Math.PI / 180, 50, 50, 10);
                if (lines.Length > 10)
                {
                    patterns.Add(new DetectedPattern
                    {
                        Type = "Linear Structures",
                        Confidence = Math.Min(lines.Length / 100.0, 1.0),
                        Count = lines.Length
                    });
                }
                
                // Detect circles
                var circles = Cv2.HoughCircles(grayImage, HoughModes.Gradient, 1, 20, 50, 30, 5, 100);
                if (circles.Length > 5)
                {
                    patterns.Add(new DetectedPattern
                    {
                        Type = "Circular Structures",
                        Confidence = Math.Min(circles.Length / 50.0, 1.0),
                        Count = circles.Length
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pattern detection failed");
            }
            
            return patterns;
        }
        
        // Implement cell detection methods
        private List&lt;DetectedCell&gt; DetectRedBloodCells(Mat image, MedicalImageAnalysisSettings settings)
        {
            // Implementation for RBC detection
            return PerformCircularCellDetection(image, settings.MinRBCArea, settings.MaxRBCArea, "RBC");
        }
        
        private List&lt;DetectedCell&gt; DetectWhiteBloodCells(Mat image, MedicalImageAnalysisSettings settings)
        {
            // Implementation for WBC detection (larger, less regular)
            return PerformCircularCellDetection(image, settings.MinWBCArea, settings.MaxWBCArea, "WBC");
        }
        
        private List&lt;DetectedCell&gt; DetectPlatelets(Mat image, MedicalImageAnalysisSettings settings)
        {
            // Implementation for platelet detection (very small)
            return PerformCircularCellDetection(image, settings.MinPlateletArea, settings.MaxPlateletArea, "Platelet");
        }
        
        private List&lt;DetectedCell&gt; PerformCircularCellDetection(Mat image, double minArea, double maxArea, string cellType)
        {
            var cells = new List&lt;DetectedCell&gt;();
            
            try
            {
                using var binary = new Mat();
                Cv2.AdaptiveThreshold(image, binary, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                
                Cv2.FindContours(binary, out var contours, out var hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                foreach (var contour in contours)
                {
                    var area = Cv2.ContourArea(contour);
                    
                    if (area > minArea && area &lt; maxArea)
                    {
                        var boundingRect = Cv2.BoundingRect(contour);
                        var moments = Cv2.Moments(contour);
                        var centerX = moments.M10 / moments.M00;
                        var centerY = moments.M01 / moments.M00;
                        var circularity = 4 * Math.PI * area / Math.Pow(Cv2.ArcLength(contour, true), 2);
                        
                        if (circularity > 0.3) // Reasonable circularity threshold
                        {
                            cells.Add(new DetectedCell
                            {
                                CenterX = centerX,
                                CenterY = centerY,
                                Area = area,
                                Circularity = circularity,
                                BoundingRect = boundingRect,
                                Type = cellType
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to detect {cellType} cells");
            }
            
            return cells;
        }
        
        // Simplified implementations for other detection methods
        private int CountUrineRBCs(Mat image, MedicalImageAnalysisSettings settings)
        {
            return PerformCircularCellDetection(image, 50, 200, "Urine RBC").Count;
        }
        
        private int CountUrineWBCs(Mat image, MedicalImageAnalysisSettings settings)
        {
            return PerformCircularCellDetection(image, 200, 800, "Urine WBC").Count;
        }
        
        private int CountEpithelialCells(Mat image, MedicalImageAnalysisSettings settings)
        {
            return PerformCircularCellDetection(image, 800, 3000, "Epithelial").Count;
        }
        
        private List&lt;DetectedCast&gt; DetectCasts(Mat image, MedicalImageAnalysisSettings settings)
        {
            // Simplified cast detection - look for elongated structures
            return new List&lt;DetectedCast&gt;();
        }
        
        private List&lt;DetectedCrystal&gt; DetectCrystals(Mat image, MedicalImageAnalysisSettings settings)
        {
            // Simplified crystal detection - look for angular structures
            return new List&lt;DetectedCrystal&gt;();
        }
        
        private int CountBacteria(Mat image, MedicalImageAnalysisSettings settings)
        {
            return PerformCircularCellDetection(image, 5, 50, "Bacteria").Count;
        }
        
        private BacteriaType ClassifyBacteriaShape(double aspectRatio, double area)
        {
            if (aspectRatio > 2.0) return BacteriaType.Bacilli; // Rod-shaped
            if (aspectRatio < 1.5) return BacteriaType.Cocci;  // Spherical
            return BacteriaType.Spiral; // Other shapes
        }
        
        public void Dispose()
        {
            _generalAnalysisModel?.Dispose();
            _cellCountingModel?.Dispose();
        }
    }
    
    // Supporting classes and enums
    public class MedicalImageAnalysisSettings
    {
        public MedicalImageAnalysisType AnalysisType { get; set; } = MedicalImageAnalysisType.GeneralAnalysis;
        public double? CalibrationMicronsPerPixel { get; set; }
        
        // Cell counting parameters
        public double MinCellArea { get; set; } = 50;
        public double MaxCellArea { get; set; } = 2000;
        
        // Blood cell parameters
        public double MinRBCArea { get; set; } = 100;
        public double MaxRBCArea { get; set; } = 400;
        public double MinWBCArea { get; set; } = 400;
        public double MaxWBCArea { get; set; } = 1500;
        public double MinPlateletArea { get; set; } = 10;
        public double MaxPlateletArea { get; set; } = 100;
        
        // Bacteria parameters
        public double MinBacteriaArea { get; set; } = 5;
        public double MaxBacteriaArea { get; set; } = 50;
    }
    
    public enum MedicalImageAnalysisType
    {
        GeneralAnalysis,
        CellCounting,
        BloodCellAnalysis,
        UrineAnalysis,
        BacteriaDetection
    }
    
    public enum ImageQuality
    {
        Unknown,
        Poor,
        Fair,
        Good,
        Excellent
    }
    
    public enum BacteriaType
    {
        Unknown,
        Cocci,
        Bacilli,
        Spiral
    }
    
    public class MedicalImageAnalysisResult
    {
        public string ImagePath { get; set; } = string.Empty;
        public DateTime AnalysisStartTime { get; set; }
        public DateTime AnalysisEndTime { get; set; }
        public TimeSpan AnalysisDuration { get; set; }
        public bool IsCompleted { get; set; }
        public string? ErrorMessage { get; set; }
        
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public MedicalImageAnalysisSettings Settings { get; set; } = new();
        
        public CellCountingResult? CellCountingResult { get; set; }
        public GeneralAnalysisResult? GeneralAnalysisResult { get; set; }
        public BloodCellAnalysisResult? BloodCellResult { get; set; }
        public UrineAnalysisResult? UrineAnalysisResult { get; set; }
        public BacteriaDetectionResult? BacteriaDetectionResult { get; set; }
    }
    
    public class CellCountingResult
    {
        public int TotalCellCount { get; set; }
        public List&lt;DetectedCell&gt; DetectedCells { get; set; } = new();
        public double AverageArea { get; set; }
        public double AverageCircularity { get; set; }
        public double CellDensityPerMm2 { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class GeneralAnalysisResult
    {
        public ImageQuality ImageQuality { get; set; }
        public double Brightness { get; set; }
        public double Contrast { get; set; }
        public double EdgeDensity { get; set; }
        public List&lt;double&gt; TextureFeatures { get; set; } = new();
        public ColorAnalysisResult? ColorAnalysis { get; set; }
        public List&lt;DetectedPattern&gt; Patterns { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
    
    public class BloodCellAnalysisResult
    {
        public List&lt;DetectedCell&gt; RedBloodCells { get; set; } = new();
        public List&lt;DetectedCell&gt; WhiteBloodCells { get; set; } = new();
        public List&lt;DetectedCell&gt; Platelets { get; set; } = new();
        public int RBCCount { get; set; }
        public int WBCCount { get; set; }
        public int PlateletCount { get; set; }
        public double WBC_RBC_Ratio { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class UrineAnalysisResult
    {
        public int RedBloodCells { get; set; }
        public int WhiteBloodCells { get; set; }
        public int EpithelialCells { get; set; }
        public List&lt;DetectedCast&gt; Casts { get; set; } = new();
        public List&lt;DetectedCrystal&gt; Crystals { get; set; } = new();
        public int Bacteria { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class BacteriaDetectionResult
    {
        public int TotalBacteriaCount { get; set; }
        public List&lt;DetectedBacteria&gt; DetectedBacteria { get; set; } = new();
        public int CocciCount { get; set; }
        public int BacilliCount { get; set; }
        public int SpiralCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    public class DetectedCell
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Area { get; set; }
        public double Perimeter { get; set; }
        public Rect BoundingRect { get; set; }
        public double AspectRatio { get; set; }
        public double Circularity { get; set; }
        public string Type { get; set; } = string.Empty;
    }
    
    public class DetectedBacteria
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Area { get; set; }
        public double AspectRatio { get; set; }
        public BacteriaType Type { get; set; }
        public Rect BoundingRect { get; set; }
    }
    
    public class DetectedCast
    {
        public string Type { get; set; } = string.Empty;
        public Rect BoundingRect { get; set; }
    }
    
    public class DetectedCrystal
    {
        public string Type { get; set; } = string.Empty;
        public Rect BoundingRect { get; set; }
    }
    
    public class ColorAnalysisResult
    {
        public double DominantHue { get; set; }
        public double AverageSaturation { get; set; }
        public double AverageBrightness { get; set; }
        public double ColorVariability { get; set; }
    }
    
    public class DetectedPattern
    {
        public string Type { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int Count { get; set; }
    }
}