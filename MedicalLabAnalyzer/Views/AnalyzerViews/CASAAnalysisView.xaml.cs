using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MedicalLabAnalyzer.ViewModels;
using Microsoft.Win32;

namespace MedicalLabAnalyzer.Views.AnalyzerViews
{
    /// <summary>
    /// CASAAnalysisView - واجهة تحليل الحيوانات المنوية باستخدام الذكاء الاصطناعي
    /// Computer Assisted Semen Analysis with YOLOv8 and DeepSORT
    /// </summary>
    public partial class CASAAnalysisView : UserControl
    {
        private CASAAnalysisViewModel _viewModel;
        private DispatcherTimer? _videoTimer;
        private bool _isDragging = false;

        public CASAAnalysisView()
        {
            InitializeComponent();
            InitializeCASAView();
        }

        /// <summary>
        /// تهيئة واجهة CASA
        /// </summary>
        private void InitializeCASAView()
        {
            try
            {
                // تطبيق ViewModel
                _viewModel = new CASAAnalysisViewModel();
                DataContext = _viewModel;

                // ربط الأحداث
                Loaded += OnCASAViewLoaded;
                Unloaded += OnCASAViewUnloaded;

                // ربط أحداث الوسائط
                VideoPlayer.MediaOpened += OnVideoOpened;
                VideoPlayer.MediaEnded += OnVideoEnded;
                VideoPlayer.MediaFailed += OnVideoFailed;

                // ربط أحداث السحب والإفلات
                InitializeDragAndDrop();

                // ربط أحداث ViewModel
                _viewModel.MediaLoadRequested += OnMediaLoadRequested;
                _viewModel.AnalysisCompleted += OnAnalysisCompleted;
                _viewModel.AnalysisProgress += OnAnalysisProgress;
                _viewModel.ErrorOccurred += OnErrorOccurred;

                // تهيئة مؤقت الفيديو
                InitializeVideoTimer();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تهيئة واجهة CASA: {ex.Message}");
            }
        }

        /// <summary>
        /// تهيئة السحب والإفلات
        /// </summary>
        private void InitializeDragAndDrop()
        {
            try
            {
                // تفعيل السحب والإفلات
                this.AllowDrop = true;
                ImageViewer.AllowDrop = true;
                VideoPlayer.AllowDrop = true;

                // ربط أحداث السحب والإفلات
                this.DragEnter += OnDragEnter;
                this.DragOver += OnDragOver;
                this.DragLeave += OnDragLeave;
                this.Drop += OnDrop;

                ImageViewer.DragEnter += OnDragEnter;
                ImageViewer.Drop += OnDrop;
                
                VideoPlayer.DragEnter += OnDragEnter;
                VideoPlayer.Drop += OnDrop;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تهيئة السحب والإفلات: {ex.Message}");
            }
        }

        /// <summary>
        /// تهيئة مؤقت الفيديو
        /// </summary>
        private void InitializeVideoTimer()
        {
            _videoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // تحديث كل 100ms
            };
            _videoTimer.Tick += OnVideoTimerTick;
        }

        /// <summary>
        /// حدث تحميل واجهة CASA
        /// </summary>
        private async void OnCASAViewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تهيئة نظام الذكاء الاصطناعي
                await _viewModel.InitializeAISystemAsync();

                // تحديد الحالة الافتراضية
                _viewModel.ShowDropZone = true;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل واجهة CASA: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث إلغاء تحميل واجهة CASA
        /// </summary>
        private void OnCASAViewUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // إيقاف مؤقت الفيديو
                _videoTimer?.Stop();

                // إيقاف الفيديو
                VideoPlayer?.Stop();

                // تنظيف الموارد
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إلغاء تحميل واجهة CASA: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث طلب تحميل الوسائط
        /// </summary>
        private void OnMediaLoadRequested(object? sender, EventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "اختر صورة أو فيديو للتحليل",
                    Filter = "ملفات الوسائط|*.jpg;*.jpeg;*.png;*.bmp;*.mp4;*.avi;*.mov;*.wmv|" +
                            "الصور|*.jpg;*.jpeg;*.png;*.bmp|" +
                            "الفيديو|*.mp4;*.avi;*.mov;*.wmv|" +
                            "جميع الملفات|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadMediaFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل الوسائط: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل ملف وسائط
        /// </summary>
        private async void LoadMediaFile(string filePath)
        {
            try
            {
                await _viewModel.LoadMediaFileAsync(filePath);
                
                // تحديث العرض بناءً على نوع الملف
                if (_viewModel.IsVideoMode)
                {
                    VideoPlayer.Source = new Uri(filePath);
                    VideoPlayer.Play();
                }
                else if (_viewModel.IsImageMode)
                {
                    // الصورة ستتحديث تلقائياً عبر Binding
                }

                _viewModel.ShowDropZone = false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل الملف: {ex.Message}");
            }
        }

        /// <summary>
        /// أحداث السحب والإفلات
        /// </summary>
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        var extension = System.IO.Path.GetExtension(files[0]).ToLower();
                        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".mp4", ".avi", ".mov", ".wmv" };
                        
                        if (Array.Exists(supportedExtensions, ext => ext == extension))
                        {
                            e.Effects = DragDropEffects.Copy;
                            _viewModel.ShowDropZone = true;
                            return;
                        }
                    }
                }
                
                e.Effects = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في DragEnter: {ex.Message}");
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            _viewModel.ShowDropZone = false;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        LoadMediaFile(files[0]);
                    }
                }
                _viewModel.ShowDropZone = false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في إفلات الملف: {ex.Message}");
            }
        }

        /// <summary>
        /// أحداث الفيديو
        /// </summary>
        private void OnVideoOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.VideoDuration = VideoPlayer.NaturalDuration.TimeSpan;
                _videoTimer?.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح الفيديو: {ex.Message}");
            }
        }

        private void OnVideoEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoTimer?.Stop();
                _viewModel.VideoPosition = TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في انتهاء الفيديو: {ex.Message}");
            }
        }

        private void OnVideoFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ShowErrorMessage($"خطأ في تشغيل الفيديو: {e.ErrorException?.Message}");
        }

        private void OnVideoTimerTick(object? sender, EventArgs e)
        {
            try
            {
                if (VideoPlayer.Source != null && VideoPlayer.NaturalDuration.HasTimeSpan && !_isDragging)
                {
                    _viewModel.VideoPosition = VideoPlayer.Position;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في مؤقت الفيديو: {ex.Message}");
            }
        }

        /// <summary>
        /// أحداث التحليل
        /// </summary>
        private void OnAnalysisCompleted(object? sender, EventArgs e)
        {
            try
            {
                // تحديث العرض مع نتائج التحليل
                _viewModel.ShowAnalysisOverlay = true;
                ShowSuccessMessage("تم إكمال التحليل بنجاح!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ بعد إكمال التحليل: {ex.Message}");
            }
        }

        private void OnAnalysisProgress(object? sender, double progress)
        {
            try
            {
                _viewModel.AnalysisProgress = progress;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث تقدم التحليل: {ex.Message}");
            }
        }

        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            ShowErrorMessage(errorMessage);
        }

        /// <summary>
        /// عرض رسائل المستخدم
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            try
            {
                MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في عرض رسالة الخطأ: {ex.Message}");
            }
        }

        private void ShowSuccessMessage(string message)
        {
            try
            {
                MessageBox.Show(message, "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في عرض رسالة النجاح: {ex.Message}");
            }
        }

        /// <summary>
        /// تنظيف الموارد
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _videoTimer?.Stop();
                VideoPlayer?.Stop();
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف موارد CASA: {ex.Message}");
            }
        }
    }
}