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
    /// ViewModel لواجهة تحليل CASA مع الذكاء الاصطناعي
    /// </summary>
    public class CASAAnalysisViewModel : BaseViewModel, IDisposable
    {
        #region Private Fields
        private readonly CASAAnalysisService _casaService;
        private readonly MediaService _mediaService;
        private readonly DatabaseService _databaseService;
        private readonly AuditService _auditService;

        private string _currentFilePath = string.Empty;
        private string _currentFileName = string.Empty;
        private string _currentFileSize = string.Empty;
        private string _mediaDimensions = string.Empty;
        private string _mediaType = string.Empty;
        
        private BitmapImage? _currentImageSource;
        private Uri? _currentVideoSource;
        
        private bool _isImageMode = false;
        private bool _isVideoMode = false;
        private bool _isVideoLoaded = false;
        private bool _showDropZone = true;
        private bool _showAnalysisOverlay = false;
        private bool _isAnalyzing = false;
        private bool _hasAnalysisResults = false;
        private bool _canStartAnalysis = false;
        
        private TimeSpan _videoDuration = TimeSpan.Zero;
        private TimeSpan _videoPosition = TimeSpan.Zero;
        private string _playPauseIcon = "Play";
        
        private double _analysisProgress = 0.0;
        private string _analysisStatusText = "جاري التحضير للتحليل...";
        private double _analysisTime = 0.0;
        private string _statusMessage = "جاهز لتحميل الوسائط";
        
        // CASA Analysis Results
        private CASAResult? _currentResults;
        private int _spermCount = 0;
        private double _motilityPercentage = 0.0;
        private double _vcl = 0.0;
        private double _vsl = 0.0;
        private double _vap = 0.0;
        private double _lin = 0.0;
        private double _str = 0.0;
        private double _wob = 0.0;
        private double _alh = 0.0;
        private double _bcf = 0.0;
        
        // AI System Status
        private bool _isYOLOv8Online = false;
        private bool _isDeepSORTOnline = false;
        private bool _isOpenCVOnline = false;
        
        private readonly ObservableCollection<DetectedSperm> _detectedSperms = new();
        private readonly ObservableCollection<SpermTrack> _spermTracks = new();
        private readonly ObservableCollection<MotilityCategory> _motilityCategories = new();
        #endregion

        #region Properties

        // File Information
        public string CurrentFileName
        {
            get => _currentFileName;
            set => SetProperty(ref _currentFileName, value);
        }

        public string CurrentFileSize
        {
            get => _currentFileSize;
            set => SetProperty(ref _currentFileSize, value);
        }

        public string MediaDimensions
        {
            get => _mediaDimensions;
            set => SetProperty(ref _mediaDimensions, value);
        }

        public string MediaType
        {
            get => _mediaType;
            set => SetProperty(ref _mediaType, value);
        }

        // Media Sources
        public BitmapImage? CurrentImageSource
        {
            get => _currentImageSource;
            set => SetProperty(ref _currentImageSource, value);
        }

        public Uri? CurrentVideoSource
        {
            get => _currentVideoSource;
            set => SetProperty(ref _currentVideoSource, value);
        }

        // Media States
        public bool IsImageMode
        {
            get => _isImageMode;
            set => SetProperty(ref _isImageMode, value);
        }

        public bool IsVideoMode
        {
            get => _isVideoMode;
            set => SetProperty(ref _isVideoMode, value);
        }

        public bool IsVideoLoaded
        {
            get => _isVideoLoaded;
            set => SetProperty(ref _isVideoLoaded, value);
        }

        public bool ShowDropZone
        {
            get => _showDropZone;
            set => SetProperty(ref _showDropZone, value);
        }

        public bool ShowAnalysisOverlay
        {
            get => _showAnalysisOverlay;
            set => SetProperty(ref _showAnalysisOverlay, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        public bool HasAnalysisResults
        {
            get => _hasAnalysisResults;
            set => SetProperty(ref _hasAnalysisResults, value);
        }

        public bool CanStartAnalysis
        {
            get => _canStartAnalysis;
            set => SetProperty(ref _canStartAnalysis, value);
        }

        // Video Control
        public TimeSpan VideoDuration
        {
            get => _videoDuration;
            set => SetProperty(ref _videoDuration, value);
        }

        public TimeSpan VideoPosition
        {
            get => _videoPosition;
            set => SetProperty(ref _videoPosition, value);
        }

        public string PlayPauseIcon
        {
            get => _playPauseIcon;
            set => SetProperty(ref _playPauseIcon, value);
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

        public double AnalysisTime
        {
            get => _analysisTime;
            set => SetProperty(ref _analysisTime, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // CASA Metrics
        public int SpermCount
        {
            get => _spermCount;
            set => SetProperty(ref _spermCount, value);
        }

        public double MotilityPercentage
        {
            get => _motilityPercentage;
            set => SetProperty(ref _motilityPercentage, value);
        }

        public double VCL
        {
            get => _vcl;
            set => SetProperty(ref _vcl, value);
        }

        public double VSL
        {
            get => _vsl;
            set => SetProperty(ref _vsl, value);
        }

        public double VAP
        {
            get => _vap;
            set => SetProperty(ref _vap, value);
        }

        public double LIN
        {
            get => _lin;
            set => SetProperty(ref _lin, value);
        }

        public double STR
        {
            get => _str;
            set => SetProperty(ref _str, value);
        }

        public double WOB
        {
            get => _wob;
            set => SetProperty(ref _wob, value);
        }

        public double ALH
        {
            get => _alh;
            set => SetProperty(ref _alh, value);
        }

        public double BCF
        {
            get => _bcf;
            set => SetProperty(ref _bcf, value);
        }

        // AI System Status
        public Brush YOLOv8StatusBrush => _isYOLOv8Online ? Brushes.Green : Brushes.Red;
        public Brush DeepSORTStatusBrush => _isDeepSORTOnline ? Brushes.Green : Brushes.Red;
        public Brush OpenCVStatusBrush => _isOpenCVOnline ? Brushes.Green : Brushes.Red;

        // Collections
        public ObservableCollection<DetectedSperm> DetectedSperms => _detectedSperms;
        public ObservableCollection<SpermTrack> SpermTracks => _spermTracks;
        public ObservableCollection<MotilityCategory> MotilityCategories => _motilityCategories;

        #endregion

        #region Commands
        public ICommand LoadMediaCommand { get; }
        public ICommand StartAnalysisCommand { get; }
        public ICommand PlayPauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand PreviousFrameCommand { get; }
        public ICommand NextFrameCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ExportPDFCommand { get; }
        public ICommand ClearResultsCommand { get; }
        #endregion

        #region Events
        public event EventHandler? MediaLoadRequested;
        public event EventHandler? AnalysisCompleted;
        public event EventHandler<double>? AnalysisProgress;
        public event EventHandler<string>? ErrorOccurred;
        #endregion

        #region Constructor
        public CASAAnalysisViewModel()
        {
            // Initialize services
            _casaService = new CASAAnalysisService();
            _mediaService = new MediaService();
            _databaseService = new DatabaseService();
            _auditService = new AuditService(_databaseService);

            // Initialize commands
            LoadMediaCommand = new AsyncRelayCommand(LoadMediaAsync);
            StartAnalysisCommand = new AsyncRelayCommand(StartAnalysisAsync, () => CanStartAnalysis);
            PlayPauseCommand = new RelayCommand(PlayPause);
            StopCommand = new RelayCommand(Stop);
            PreviousFrameCommand = new RelayCommand(PreviousFrame);
            NextFrameCommand = new RelayCommand(NextFrame);
            SaveResultsCommand = new AsyncRelayCommand(SaveResultsAsync, () => HasAnalysisResults);
            ExportPDFCommand = new AsyncRelayCommand(ExportPDFAsync, () => HasAnalysisResults);
            ClearResultsCommand = new RelayCommand(ClearResults, () => HasAnalysisResults);

            // Initialize motility categories
            InitializeMotilityCategories();
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
                StatusMessage = "جاري تهيئة نظام الذكاء الاصطناعي...";

                // Check YOLOv8
                _isYOLOv8Online = await _casaService.CheckYOLOv8StatusAsync();
                OnPropertyChanged(nameof(YOLOv8StatusBrush));

                // Check DeepSORT
                _isDeepSORTOnline = await _casaService.CheckDeepSORTStatusAsync();
                OnPropertyChanged(nameof(DeepSORTStatusBrush));

                // Check OpenCV
                _isOpenCVOnline = await _casaService.CheckOpenCVStatusAsync();
                OnPropertyChanged(nameof(OpenCVStatusBrush));

                var onlineCount = new[] { _isYOLOv8Online, _isDeepSORTOnline, _isOpenCVOnline }.Count(x => x);
                StatusMessage = $"نظام الذكاء الاصطناعي جاهز ({onlineCount}/3 أنظمة متاحة)";

                await _auditService.LogAsync("CASA_AI_INIT", $"AI System initialized: YOLOv8={_isYOLOv8Online}, DeepSORT={_isDeepSORTOnline}, OpenCV={_isOpenCVOnline}");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تهيئة نظام الذكاء الاصطناعي";
                OnErrorOccurred($"خطأ في تهيئة نظام الذكاء الاصطناعي: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل ملف وسائط
        /// </summary>
        public async Task LoadMediaFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("الملف غير موجود");
                }

                _currentFilePath = filePath;
                CurrentFileName = Path.GetFileName(filePath);
                
                var fileInfo = new FileInfo(filePath);
                CurrentFileSize = FormatFileSize(fileInfo.Length);

                var extension = Path.GetExtension(filePath).ToLower();
                var isVideo = new[] { ".mp4", ".avi", ".mov", ".wmv" }.Contains(extension);
                var isImage = new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(extension);

                if (isVideo)
                {
                    await LoadVideoAsync(filePath);
                }
                else if (isImage)
                {
                    await LoadImageAsync(filePath);
                }
                else
                {
                    throw new NotSupportedException("نوع الملف غير مدعوم");
                }

                CanStartAnalysis = true;
                StatusMessage = $"تم تحميل {CurrentFileName} بنجاح";

                await _auditService.LogAsync("CASA_MEDIA_LOADED", $"Media file loaded: {CurrentFileName} ({CurrentFileSize})");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تحميل الملف";
                OnErrorOccurred($"خطأ في تحميل الملف: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// تحميل صورة
        /// </summary>
        private async Task LoadImageAsync(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            CurrentImageSource = bitmap;
            IsImageMode = true;
            IsVideoMode = false;
            MediaType = "صورة";
            MediaDimensions = $"{bitmap.PixelWidth} × {bitmap.PixelHeight}";

            await Task.CompletedTask;
        }

        /// <summary>
        /// تحميل فيديو
        /// </summary>
        private async Task LoadVideoAsync(string filePath)
        {
            CurrentVideoSource = new Uri(filePath);
            IsVideoMode = true;
            IsImageMode = false;
            MediaType = "فيديو";

            // سيتم تحديد الأبعاد عند فتح الفيديو
            MediaDimensions = "جاري التحميل...";

            await Task.CompletedTask;
        }

        /// <summary>
        /// تحميل الوسائط
        /// </summary>
        private async Task LoadMediaAsync()
        {
            MediaLoadRequested?.Invoke(this, EventArgs.Empty);
            await Task.CompletedTask;
        }

        /// <summary>
        /// بدء التحليل
        /// </summary>
        private async Task StartAnalysisAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath))
                {
                    OnErrorOccurred("لم يتم تحديد ملف للتحليل");
                    return;
                }

                IsAnalyzing = true;
                AnalysisProgress = 0.0;
                AnalysisStatusText = "جاري بدء التحليل...";
                StatusMessage = "جاري تحليل الوسائط...";

                var startTime = DateTime.Now;

                // Progress callback
                var progress = new Progress<double>(value =>
                {
                    AnalysisProgress = value;
                    AnalysisStatusText = value switch
                    {
                        var v when v < 20 => "جاري تحضير البيانات...",
                        var v when v < 40 => "جاري كشف الحيوانات المنوية...",
                        var v when v < 60 => "جاري تتبع الحركة...",
                        var v when v < 80 => "جاري حساب المعايير...",
                        var v when v < 100 => "جاري إنهاء التحليل...",
                        _ => "مكتمل"
                    };
                });

                // Start analysis
                _currentResults = await _casaService.AnalyzeAsync(_currentFilePath, progress);

                // Update results
                if (_currentResults != null)
                {
                    UpdateAnalysisResults(_currentResults);
                    HasAnalysisResults = true;
                }

                var endTime = DateTime.Now;
                AnalysisTime = (endTime - startTime).TotalSeconds;

                StatusMessage = "تم إكمال التحليل بنجاح";
                IsAnalyzing = false;

                AnalysisCompleted?.Invoke(this, EventArgs.Empty);

                await _auditService.LogAsync("CASA_ANALYSIS_COMPLETED", 
                    $"CASA analysis completed for {CurrentFileName}. " +
                    $"Results: Count={SpermCount}, Motility={MotilityPercentage:F1}%, Time={AnalysisTime:F1}s");
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                StatusMessage = "خطأ في التحليل";
                OnErrorOccurred($"خطأ في التحليل: {ex.Message}");
            }
        }

        /// <summary>
        /// تحديث نتائج التحليل
        /// </summary>
        private void UpdateAnalysisResults(CASAResult results)
        {
            SpermCount = results.SpermCount;
            MotilityPercentage = results.MotilityPercentage;
            VCL = results.VCL;
            VSL = results.VSL;
            VAP = results.VAP;
            LIN = results.LIN;
            STR = results.STR;
            WOB = results.WOB;
            ALH = results.ALH;
            BCF = results.BCF;

            // Update detected sperms
            _detectedSperms.Clear();
            if (results.DetectedSperms != null)
            {
                foreach (var sperm in results.DetectedSperms)
                {
                    _detectedSperms.Add(sperm);
                }
            }

            // Update sperm tracks
            _spermTracks.Clear();
            if (results.SpermTracks != null)
            {
                foreach (var track in results.SpermTracks)
                {
                    _spermTracks.Add(track);
                }
            }

            // Update motility categories
            UpdateMotilityCategories(results);
        }

        /// <summary>
        /// تحديث فئات الحركة
        /// </summary>
        private void UpdateMotilityCategories(CASAResult results)
        {
            _motilityCategories.Clear();

            var categories = new[]
            {
                new MotilityCategory
                {
                    CategoryName = "سريع التقدم",
                    Description = "حركة سريعة مستقيمة",
                    Count = results.ProgressiveRapidCount,
                    Percentage = results.ProgressiveRapidPercentage,
                    CategoryColor = new SolidColorBrush(Colors.Green)
                },
                new MotilityCategory
                {
                    CategoryName = "بطيء التقدم",
                    Description = "حركة بطيئة مستقيمة",
                    Count = results.ProgressiveSlowCount,
                    Percentage = results.ProgressiveSlowPercentage,
                    CategoryColor = new SolidColorBrush(Colors.Orange)
                },
                new MotilityCategory
                {
                    CategoryName = "غير متقدم",
                    Description = "حركة في المكان",
                    Count = results.NonProgressiveCount,
                    Percentage = results.NonProgressivePercentage,
                    CategoryColor = new SolidColorBrush(Colors.Yellow)
                },
                new MotilityCategory
                {
                    CategoryName = "ساكن",
                    Description = "لا توجد حركة",
                    Count = results.ImmotileCount,
                    Percentage = results.ImmotilePercentage,
                    CategoryColor = new SolidColorBrush(Colors.Red)
                }
            };

            foreach (var category in categories)
            {
                _motilityCategories.Add(category);
            }
        }

        /// <summary>
        /// تهيئة فئات الحركة الافتراضية
        /// </summary>
        private void InitializeMotilityCategories()
        {
            _motilityCategories.Add(new MotilityCategory
            {
                CategoryName = "سريع التقدم",
                Description = "حركة سريعة مستقيمة",
                Count = 0,
                Percentage = 0.0,
                CategoryColor = new SolidColorBrush(Colors.Green)
            });
            _motilityCategories.Add(new MotilityCategory
            {
                CategoryName = "بطيء التقدم", 
                Description = "حركة بطيئة مستقيمة",
                Count = 0,
                Percentage = 0.0,
                CategoryColor = new SolidColorBrush(Colors.Orange)
            });
            _motilityCategories.Add(new MotilityCategory
            {
                CategoryName = "غير متقدم",
                Description = "حركة في المكان",
                Count = 0,
                Percentage = 0.0,
                CategoryColor = new SolidColorBrush(Colors.Yellow)
            });
            _motilityCategories.Add(new MotilityCategory
            {
                CategoryName = "ساكن",
                Description = "لا توجد حركة",
                Count = 0,
                Percentage = 0.0,
                CategoryColor = new SolidColorBrush(Colors.Red)
            });
        }

        /// <summary>
        /// أوامر التحكم في الفيديو
        /// </summary>
        private void PlayPause()
        {
            // Will be handled by the view
        }

        private void Stop()
        {
            // Will be handled by the view
        }

        private void PreviousFrame()
        {
            // Will be handled by the view
        }

        private void NextFrame()
        {
            // Will be handled by the view
        }

        /// <summary>
        /// حفظ النتائج
        /// </summary>
        private async Task SaveResultsAsync()
        {
            try
            {
                if (_currentResults == null)
                {
                    OnErrorOccurred("لا توجد نتائج لحفظها");
                    return;
                }

                // Save to database
                await _databaseService.SaveCASAResultAsync(_currentResults);
                
                StatusMessage = "تم حفظ النتائج بنجاح";
                await _auditService.LogAsync("CASA_RESULTS_SAVED", $"CASA results saved for {CurrentFileName}");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في حفظ النتائج";
                OnErrorOccurred($"خطأ في حفظ النتائج: {ex.Message}");
            }
        }

        /// <summary>
        /// تصدير تقرير PDF
        /// </summary>
        private async Task ExportPDFAsync()
        {
            try
            {
                if (_currentResults == null)
                {
                    OnErrorOccurred("لا توجد نتائج لتصديرها");
                    return;
                }

                // Generate PDF report
                var reportPath = await _casaService.GeneratePDFReportAsync(_currentResults, CurrentFileName);
                
                StatusMessage = $"تم تصدير التقرير إلى: {reportPath}";
                await _auditService.LogAsync("CASA_REPORT_EXPORTED", $"PDF report exported for {CurrentFileName}");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تصدير التقرير";
                OnErrorOccurred($"خطأ في تصدير التقرير: {ex.Message}");
            }
        }

        /// <summary>
        /// مسح النتائج
        /// </summary>
        private void ClearResults()
        {
            try
            {
                _currentResults = null;
                HasAnalysisResults = false;
                ShowAnalysisOverlay = false;
                
                SpermCount = 0;
                MotilityPercentage = 0.0;
                VCL = VSL = VAP = LIN = STR = WOB = ALH = BCF = 0.0;
                
                _detectedSperms.Clear();
                _spermTracks.Clear();
                
                InitializeMotilityCategories();
                
                StatusMessage = "تم مسح النتائج";
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"خطأ في مسح النتائج: {ex.Message}");
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
                _casaService?.Dispose();
                _mediaService?.Dispose();
                _databaseService?.Dispose();
                _auditService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف CASAAnalysisViewModel: {ex.Message}");
            }
        }
        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// حيوان منوي مكتشف
    /// </summary>
    public class DetectedSperm
    {
        public int SpermId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Confidence { get; set; }
        public string Status { get; set; } = string.Empty;
        public Brush StatusColor { get; set; } = Brushes.Green;
    }

    /// <summary>
    /// مسار حركة الحيوان المنوي
    /// </summary>
    public class SpermTrack
    {
        public int TrackId { get; set; }
        public PointCollection TrackPoints { get; set; } = new();
        public Brush TrackColor { get; set; } = Brushes.Blue;
    }

    /// <summary>
    /// فئة الحركة
    /// </summary>
    public class MotilityCategory
    {
        public string CategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public Brush CategoryColor { get; set; } = Brushes.Gray;
    }

    #endregion
}