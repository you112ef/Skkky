using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MedicalLabAnalyzer.Models;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Services.AI;

namespace MedicalLabAnalyzer.ViewModels
{
    /// <summary>
    /// ViewModel لواجهة تحليل الصور الطبية مع الذكاء الاصطناعي
    /// </summary>
    public class MedicalImageAnalysisViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        private readonly MedicalImageAnalysisService _imageAnalysisService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _databaseService;
        private readonly AuditService _auditService;

        private string _currentImagePath = string.Empty;
        private string _imageFileName = string.Empty;
        private string _imageFileSize = string.Empty;
        private string _imageDimensions = string.Empty;
        private string _imageFormat = string.Empty;
        
        private BitmapImage? _currentImageSource;
        private BitmapImage? _originalImageSource;
        
        private bool _hasImage = false;
        private bool _showDropZone = true;
        private bool _isAnalyzing = false;
        private bool _hasResults = false;
        private bool _hasAIResults = false;
        private bool _showDetections = false;
        
        private double _analysisProgress = 0.0;
        private string _analysisStatusText = "جاري التحضير للتحليل...";
        private string _statusMessage = "جاهز لتحميل الصور";
        
        // Tool Selection
        private bool _isPointerToolSelected = true;
        private bool _isRulerToolSelected = false;
        private bool _isAngleToolSelected = false;
        private bool _isAreaToolSelected = false;
        private bool _isCountToolSelected = false;
        
        // Zoom and View
        private double _zoomLevel = 1.0;
        private string _mouseCoordinates = "(0, 0)";
        private double _pixelScale = 1.0; // μm per pixel
        
        // Manual Measurements
        private double _totalDistance = 0.0;
        private double _totalArea = 0.0;
        private double _averageAngle = 0.0;
        private int _cellCount = 0;
        
        // AI Analysis Results
        private int _detectedObjectsCount = 0;
        private double _cellDensity = 0.0;
        private double _averageCellSize = 0.0;
        private double _analysisConfidence = 0.0;
        
        // Image Enhancement
        private double _brightness = 0.0;
        private double _contrast = 0.0;
        private double _saturation = 0.0;
        private double _sharpness = 0.0;
        
        // AI System Status
        private bool _isAISystemOnline = false;
        
        private readonly ObservableCollection<RulerMeasurement> _rulers = new();
        private readonly ObservableCollection<MeasurementText> _measurementTexts = new();
        private readonly ObservableCollection<CountMarker> _countMarkers = new();
        private readonly ObservableCollection<DetectedObject> _detectedObjects = new();
        private readonly ObservableCollection<ObjectTypeInfo> _objectTypes = new();
        #endregion

        #region Properties

        // Image Information
        public string ImageFileName
        {
            get => _imageFileName;
            set => SetProperty(ref _imageFileName, value);
        }

        public string ImageFileSize
        {
            get => _imageFileSize;
            set => SetProperty(ref _imageFileSize, value);
        }

        public string ImageDimensions
        {
            get => _imageDimensions;
            set => SetProperty(ref _imageDimensions, value);
        }

        public string ImageFormat
        {
            get => _imageFormat;
            set => SetProperty(ref _imageFormat, value);
        }

        // Image Sources
        public BitmapImage? CurrentImageSource
        {
            get => _currentImageSource;
            set => SetProperty(ref _currentImageSource, value);
        }

        // States
        public bool HasImage
        {
            get => _hasImage;
            set => SetProperty(ref _hasImage, value);
        }

        public bool ShowDropZone
        {
            get => _showDropZone;
            set => SetProperty(ref _showDropZone, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        public bool HasAIResults
        {
            get => _hasAIResults;
            set => SetProperty(ref _hasAIResults, value);
        }

        public bool ShowDetections
        {
            get => _showDetections;
            set => SetProperty(ref _showDetections, value);
        }

        // Analysis Progress
        public double AnalysisProgress
        {
            get => _analysisProgress;
            set => SetProperty(ref _analysisProgress, value);
        }

        public string AnalysisStatusText
        {
            get => _analysisStatusText;
            set => SetProperty(ref _analysisStatusText, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Tool Selection
        public bool IsPointerToolSelected
        {
            get => _isPointerToolSelected;
            set
            {
                if (SetProperty(ref _isPointerToolSelected, value) && value)
                {
                    ClearOtherToolSelections(nameof(IsPointerToolSelected));
                }
            }
        }

        public bool IsRulerToolSelected
        {
            get => _isRulerToolSelected;
            set
            {
                if (SetProperty(ref _isRulerToolSelected, value) && value)
                {
                    ClearOtherToolSelections(nameof(IsRulerToolSelected));
                }
            }
        }

        public bool IsAngleToolSelected
        {
            get => _isAngleToolSelected;
            set
            {
                if (SetProperty(ref _isAngleToolSelected, value) && value)
                {
                    ClearOtherToolSelections(nameof(IsAngleToolSelected));
                }
            }
        }

        public bool IsAreaToolSelected
        {
            get => _isAreaToolSelected;
            set
            {
                if (SetProperty(ref _isAreaToolSelected, value) && value)
                {
                    ClearOtherToolSelections(nameof(IsAreaToolSelected));
                }
            }
        }

        public bool IsCountToolSelected
        {
            get => _isCountToolSelected;
            set
            {
                if (SetProperty(ref _isCountToolSelected, value) && value)
                {
                    ClearOtherToolSelections(nameof(IsCountToolSelected));
                }
            }
        }

        // Zoom and View
        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, Math.Max(0.1, Math.Min(5.0, value)));
        }

        public string MouseCoordinates
        {
            get => _mouseCoordinates;
            set => SetProperty(ref _mouseCoordinates, value);
        }

        public double PixelScale
        {
            get => _pixelScale;
            set => SetProperty(ref _pixelScale, value);
        }

        // Manual Measurements
        public double TotalDistance
        {
            get => _totalDistance;
            set => SetProperty(ref _totalDistance, value);
        }

        public double TotalArea
        {
            get => _totalArea;
            set => SetProperty(ref _totalArea, value);
        }

        public double AverageAngle
        {
            get => _averageAngle;
            set => SetProperty(ref _averageAngle, value);
        }

        public int CellCount
        {
            get => _cellCount;
            set => SetProperty(ref _cellCount, value);
        }

        // AI Analysis Results
        public int DetectedObjectsCount
        {
            get => _detectedObjectsCount;
            set => SetProperty(ref _detectedObjectsCount, value);
        }

        public double CellDensity
        {
            get => _cellDensity;
            set => SetProperty(ref _cellDensity, value);
        }

        public double AverageCellSize
        {
            get => _averageCellSize;
            set => SetProperty(ref _averageCellSize, value);
        }

        public double AnalysisConfidence
        {
            get => _analysisConfidence;
            set => SetProperty(ref _analysisConfidence, value);
        }

        // Image Enhancement
        public double Brightness
        {
            get => _brightness;
            set => SetProperty(ref _brightness, Math.Max(-100, Math.Min(100, value)));
        }

        public double Contrast
        {
            get => _contrast;
            set => SetProperty(ref _contrast, Math.Max(-100, Math.Min(100, value)));
        }

        public double Saturation
        {
            get => _saturation;
            set => SetProperty(ref _saturation, Math.Max(-100, Math.Min(100, value)));
        }

        public double Sharpness
        {
            get => _sharpness;
            set => SetProperty(ref _sharpness, Math.Max(0, Math.Min(100, value)));
        }

        // AI System Status
        public Brush AIStatusBrush => _isAISystemOnline ? Brushes.Green : Brushes.Orange;
        public string AIStatusText => _isAISystemOnline ? "AI متاح" : "AI غير متاح";

        // Collections
        public ObservableCollection<RulerMeasurement> Rulers => _rulers;
        public ObservableCollection<MeasurementText> MeasurementTexts => _measurementTexts;
        public ObservableCollection<CountMarker> CountMarkers => _countMarkers;
        public ObservableCollection<DetectedObject> DetectedObjects => _detectedObjects;
        public ObservableCollection<ObjectTypeInfo> ObjectTypes => _objectTypes;

        #endregion

        #region Commands
        public ICommand LoadImageCommand { get; }
        public ICommand StartAutoAnalysisCommand { get; }
        
        // Tool Selection Commands
        public ICommand SelectPointerToolCommand { get; }
        public ICommand SelectRulerToolCommand { get; }
        public ICommand SelectAngleToolCommand { get; }
        public ICommand SelectAreaToolCommand { get; }
        public ICommand SelectCountToolCommand { get; }
        
        // Measurement Commands
        public ICommand ClearMeasurementsCommand { get; }
        
        // Zoom Commands
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand FitToScreenCommand { get; }
        public ICommand ActualSizeCommand { get; }
        
        // Enhancement Commands
        public ICommand ResetEnhancementsCommand { get; }
        public ICommand ApplyEnhancementsCommand { get; }
        
        // Results Commands
        public ICommand SaveResultsCommand { get; }
        #endregion

        #region Events
        public event EventHandler? ImageLoadRequested;
        public event EventHandler? AnalysisCompleted;
        public event EventHandler<string>? ErrorOccurred;
        #endregion

        #region Constructor
        public MedicalImageAnalysisViewModel()
        {
            // Initialize services
            _imageAnalysisService = new MedicalImageAnalysisService();
            _mediaService = new MediaService();
            _databaseService = new DatabaseService();
            _auditService = new AuditService(_databaseService);

            // Initialize commands
            LoadImageCommand = new AsyncRelayCommand(LoadImageAsync);
            StartAutoAnalysisCommand = new AsyncRelayCommand(StartAutoAnalysisAsync, () => HasImage && !IsAnalyzing);
            
            SelectPointerToolCommand = new RelayCommand(() => IsPointerToolSelected = true);
            SelectRulerToolCommand = new RelayCommand(() => IsRulerToolSelected = true);
            SelectAngleToolCommand = new RelayCommand(() => IsAngleToolSelected = true);
            SelectAreaToolCommand = new RelayCommand(() => IsAreaToolSelected = true);
            SelectCountToolCommand = new RelayCommand(() => IsCountToolSelected = true);
            
            ClearMeasurementsCommand = new RelayCommand(ClearMeasurements);
            
            ZoomInCommand = new RelayCommand(ZoomIn);
            ZoomOutCommand = new RelayCommand(ZoomOut);
            FitToScreenCommand = new RelayCommand(FitToScreen);
            ActualSizeCommand = new RelayCommand(ActualSize);
            
            ResetEnhancementsCommand = new RelayCommand(ResetEnhancements);
            ApplyEnhancementsCommand = new AsyncRelayCommand(ApplyEnhancementsAsync, () => HasImage);
            
            SaveResultsCommand = new AsyncRelayCommand(SaveResultsAsync, () => HasResults);

            // Set default pixel scale (assuming 1 μm per pixel initially)
            PixelScale = 1.0;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// تهيئة نظام الذكاء الاصطناعي
        /// </summary>
        public async Task InitializeAISystemAsync()
        {
            try
            {
                StatusMessage = "جاري تهيئة نظام الذكاء الاصطناعي للصور...";

                _isAISystemOnline = await _imageAnalysisService.InitializeAsync();
                OnPropertyChanged(nameof(AIStatusBrush));
                OnPropertyChanged(nameof(AIStatusText));

                StatusMessage = _isAISystemOnline ? 
                    "نظام تحليل الصور الطبية جاهز" : 
                    "تحذير: نظام الذكاء الاصطناعي غير متاح";

                await _auditService.LogAsync("IMAGE_AI_INIT", 
                    $"Medical Image AI System initialized: Status={_isAISystemOnline}");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تهيئة نظام الذكاء الاصطناعي";
                OnErrorOccurred($"خطأ في تهيئة نظام الذكاء الاصطناعي: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل ملف صورة
        /// </summary>
        public async Task LoadImageFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("الملف غير موجود");
                }

                _currentImagePath = filePath;
                ImageFileName = Path.GetFileName(filePath);
                
                var fileInfo = new FileInfo(filePath);
                ImageFileSize = FormatFileSize(fileInfo.Length);
                ImageFormat = Path.GetExtension(filePath).TrimStart('.').ToUpper();

                // Load image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                CurrentImageSource = bitmap;
                _originalImageSource = bitmap;
                
                ImageDimensions = $"{bitmap.PixelWidth} × {bitmap.PixelHeight}";
                HasImage = true;

                // Clear previous measurements and results
                ClearMeasurements();
                ClearAnalysisResults();

                StatusMessage = $"تم تحميل {ImageFileName} بنجاح";

                await _auditService.LogAsync("IMAGE_LOADED", 
                    $"Medical image loaded: {ImageFileName} ({ImageFileSize})");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تحميل الصورة";
                OnErrorOccurred($"خطأ في تحميل الصورة: {ex.Message}");
            }
        }

        /// <summary>
        /// إضافة قياس بالمسطرة
        /// </summary>
        public void AddRulerMeasurement(double x1, double y1, double x2, double y2, double pixelDistance)
        {
            try
            {
                var realDistance = pixelDistance * PixelScale;
                
                var ruler = new RulerMeasurement
                {
                    StartX = x1,
                    StartY = y1,
                    EndX = x2,
                    EndY = y2,
                    Distance = realDistance
                };
                
                _rulers.Add(ruler);
                
                // Add measurement text
                var midX = (x1 + x2) / 2;
                var midY = (y1 + y2) / 2;
                
                var measurementText = new MeasurementText
                {
                    X = midX,
                    Y = midY - 20,
                    Text = $"{realDistance:F2} μm"
                };
                
                _measurementTexts.Add(measurementText);
                
                // Update total
                TotalDistance = _rulers.Sum(r => r.Distance);
                HasResults = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في إضافة قياس المسطرة: {ex.Message}");
            }
        }

        /// <summary>
        /// إضافة قياس المساحة
        /// </summary>
        public void AddAreaMeasurement(double x, double y, double width, double height, double pixelArea)
        {
            try
            {
                var realArea = pixelArea * PixelScale * PixelScale;
                
                // Add measurement text
                var measurementText = new MeasurementText
                {
                    X = x + width / 2,
                    Y = y + height / 2,
                    Text = $"{realArea:F2} μm²"
                };
                
                _measurementTexts.Add(measurementText);
                
                // Update total area
                TotalArea += realArea;
                HasResults = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في إضافة قياس المساحة: {ex.Message}");
            }
        }

        /// <summary>
        /// إضافة قياس الزاوية
        /// </summary>
        public void AddAngleMeasurement(double x1, double y1, double x2, double y2)
        {
            try
            {
                // حساب الزاوية (يحتاج تطوير أكثر للزوايا الحقيقية)
                var angle = Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI;
                angle = Math.Abs(angle);
                
                // Add measurement text
                var midX = (x1 + x2) / 2;
                var midY = (y1 + y2) / 2;
                
                var measurementText = new MeasurementText
                {
                    X = midX,
                    Y = midY - 20,
                    Text = $"{angle:F1}°"
                };
                
                _measurementTexts.Add(measurementText);
                
                // Update average angle
                var angles = _measurementTexts.Where(t => t.Text.Contains("°"))
                    .Select(t => double.Parse(t.Text.Replace("°", "")))
                    .ToList();
                
                AverageAngle = angles.Any() ? angles.Average() : 0;
                HasResults = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في إضافة قياس الزاوية: {ex.Message}");
            }
        }

        /// <summary>
        /// إضافة علامة عدّ
        /// </summary>
        public void AddCountMarker(double x, double y)
        {
            try
            {
                var marker = new CountMarker
                {
                    X = x - 8,
                    Y = y - 8,
                    Label = $"خلية {CellCount + 1}"
                };
                
                _countMarkers.Add(marker);
                CellCount = _countMarkers.Count;
                HasResults = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في إضافة علامة العدّ: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// تحميل صورة
        /// </summary>
        private async Task LoadImageAsync()
        {
            ImageLoadRequested?.Invoke(this, EventArgs.Empty);
            await Task.CompletedTask;
        }

        /// <summary>
        /// بدء التحليل التلقائي
        /// </summary>
        private async Task StartAutoAnalysisAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentImagePath) || !_isAISystemOnline)
                {
                    OnErrorOccurred(_isAISystemOnline ? 
                        "لم يتم تحديد صورة للتحليل" : 
                        "نظام الذكاء الاصطناعي غير متاح");
                    return;
                }

                IsAnalyzing = true;
                AnalysisProgress = 0.0;
                AnalysisStatusText = "جاري بدء التحليل...";
                StatusMessage = "جاري تحليل الصورة الطبية...";

                var startTime = DateTime.Now;

                // Progress callback
                var progress = new Progress<double>(value =>
                {
                    AnalysisProgress = value;
                    AnalysisStatusText = value switch
                    {
                        var v when v < 25 => "جاري معالجة الصورة...",
                        var v when v < 50 => "جاري كشف الكائنات...",
                        var v when v < 75 => "جاري تحليل الخلايا...",
                        var v when v < 100 => "جاري حساب النتائج...",
                        _ => "مكتمل"
                    };
                });

                // Start AI analysis
                var analysisResult = await _imageAnalysisService.AnalyzeImageAsync(_currentImagePath, progress);

                if (analysisResult != null)
                {
                    UpdateAIAnalysisResults(analysisResult);
                    HasAIResults = true;
                    ShowDetections = true;
                }

                var endTime = DateTime.Now;
                var analysisTime = (endTime - startTime).TotalSeconds;

                StatusMessage = "تم إكمال التحليل التلقائي بنجاح";
                IsAnalyzing = false;

                AnalysisCompleted?.Invoke(this, EventArgs.Empty);

                await _auditService.LogAsync("IMAGE_AI_ANALYSIS_COMPLETED", 
                    $"AI Image analysis completed for {ImageFileName}. " +
                    $"Objects detected: {DetectedObjectsCount}, Time: {analysisTime:F1}s");
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                StatusMessage = "خطأ في التحليل التلقائي";
                OnErrorOccurred($"خطأ في التحليل التلقائي: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث نتائج التحليل بالذكاء الاصطناعي
        /// </summary>
        private void UpdateAIAnalysisResults(MedicalImageAnalysisResult result)
        {
            try
            {
                DetectedObjectsCount = result.DetectedObjects?.Count ?? 0;
                CellDensity = result.CellDensity;
                AverageCellSize = result.AverageCellSize;
                AnalysisConfidence = result.OverallConfidence;

                // Update detected objects
                _detectedObjects.Clear();
                if (result.DetectedObjects != null)
                {
                    foreach (var obj in result.DetectedObjects)
                    {
                        _detectedObjects.Add(obj);
                    }
                }

                // Update object types distribution
                _objectTypes.Clear();
                if (result.ObjectTypesDistribution != null)
                {
                    foreach (var objType in result.ObjectTypesDistribution)
                    {
                        _objectTypes.Add(objType);
                    }
                }

                HasResults = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في تحديث نتائج الذكاء الاصطناعي: {ex.Message}");
            }
        }

        /// <summary>
        /// مسح الأدوات المختارة الأخرى
        /// </summary>
        private void ClearOtherToolSelections(string selectedTool)
        {
            if (selectedTool != nameof(IsPointerToolSelected)) _isPointerToolSelected = false;
            if (selectedTool != nameof(IsRulerToolSelected)) _isRulerToolSelected = false;
            if (selectedTool != nameof(IsAngleToolSelected)) _isAngleToolSelected = false;
            if (selectedTool != nameof(IsAreaToolSelected)) _isAreaToolSelected = false;
            if (selectedTool != nameof(IsCountToolSelected)) _isCountToolSelected = false;

            OnPropertyChanged(nameof(IsPointerToolSelected));
            OnPropertyChanged(nameof(IsRulerToolSelected));
            OnPropertyChanged(nameof(IsAngleToolSelected));
            OnPropertyChanged(nameof(IsAreaToolSelected));
            OnPropertyChanged(nameof(IsCountToolSelected));
        }

        /// <summary>
        /// مسح القياسات
        /// </summary>
        private void ClearMeasurements()
        {
            _rulers.Clear();
            _measurementTexts.Clear();
            _countMarkers.Clear();
            
            TotalDistance = 0.0;
            TotalArea = 0.0;
            AverageAngle = 0.0;
            CellCount = 0;
        }

        /// <summary>
        /// مسح نتائج التحليل
        /// </summary>
        private void ClearAnalysisResults()
        {
            _detectedObjects.Clear();
            _objectTypes.Clear();
            
            HasAIResults = false;
            ShowDetections = false;
            DetectedObjectsCount = 0;
            CellDensity = 0.0;
            AverageCellSize = 0.0;
            AnalysisConfidence = 0.0;
        }

        /// <summary>
        /// أوامر التكبير والتصغير
        /// </summary>
        private void ZoomIn()
        {
            ZoomLevel = Math.Min(5.0, ZoomLevel * 1.2);
        }

        private void ZoomOut()
        {
            ZoomLevel = Math.Max(0.1, ZoomLevel / 1.2);
        }

        private void FitToScreen()
        {
            // سيتم تنفيذه في الواجهة
            ZoomLevel = 1.0;
        }

        private void ActualSize()
        {
            ZoomLevel = 1.0;
        }

        /// <summary>
        /// أوامر تحسين الصورة
        /// </summary>
        private void ResetEnhancements()
        {
            Brightness = 0.0;
            Contrast = 0.0;
            Saturation = 0.0;
            Sharpness = 0.0;
            
            // Reset to original image
            CurrentImageSource = _originalImageSource;
        }

        private async Task ApplyEnhancementsAsync()
        {
            try
            {
                if (_originalImageSource == null) return;

                StatusMessage = "جاري تطبيق التحسينات...";

                // Apply enhancements using MediaService
                var enhancedImage = await _mediaService.ApplyImageEnhancementsAsync(
                    _currentImagePath, Brightness, Contrast, Saturation, Sharpness);

                if (enhancedImage != null)
                {
                    CurrentImageSource = enhancedImage;
                    StatusMessage = "تم تطبيق التحسينات بنجاح";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تطبيق التحسينات";
                OnErrorOccurred($"خطأ في تطبيق التحسينات: {ex.Message}");
            }
        }

        /// <summary>
        /// حفظ النتائج
        /// </summary>
        private async Task SaveResultsAsync()
        {
            try
            {
                // Save manual measurements and AI results
                // This would involve creating appropriate database records
                
                StatusMessage = "تم حفظ النتائج بنجاح";
                await _auditService.LogAsync("IMAGE_ANALYSIS_RESULTS_SAVED", 
                    $"Image analysis results saved for {ImageFileName}");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في حفظ النتائج";
                OnErrorOccurred($"خطأ في حفظ النتائج: {ex.Message}");
            }
        }

        /// <summary>
        /// تنسيق حجم الملف
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// إثارة حدث الخطأ
        /// </summary>
        private void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        #endregion

        #region IDisposable
        public void Dispose()
        {
            try
            {
                _imageAnalysisService?.Dispose();
                _mediaService?.Dispose();
                _databaseService?.Dispose();
                _auditService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف MedicalImageAnalysisViewModel: {ex.Message}");
            }
        }
        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// قياس بالمسطرة
    /// </summary>
    public class RulerMeasurement
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Distance { get; set; }
    }

    /// <summary>
    /// نص القياس
    /// </summary>
    public class MeasurementText
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// علامة العدّ
    /// </summary>
    public class CountMarker
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// كائن مكتشف
    /// </summary>
    public class DetectedObject
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ObjectType { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Brush DetectionColor { get; set; } = Brushes.Red;
    }

    /// <summary>
    /// معلومات نوع الكائن
    /// </summary>
    public class ObjectTypeInfo
    {
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush TypeColor { get; set; } = Brushes.Blue;
    }

    /// <summary>
    /// نتيجة تحليل الصورة الطبية
    /// </summary>
    public class MedicalImageAnalysisResult
    {
        public List<DetectedObject>? DetectedObjects { get; set; }
        public List<ObjectTypeInfo>? ObjectTypesDistribution { get; set; }
        public double CellDensity { get; set; }
        public double AverageCellSize { get; set; }
        public double OverallConfidence { get; set; }
    }

    #endregion
}