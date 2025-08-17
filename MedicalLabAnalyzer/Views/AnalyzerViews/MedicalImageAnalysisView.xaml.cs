using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MedicalLabAnalyzer.ViewModels;
using Microsoft.Win32;

namespace MedicalLabAnalyzer.Views.AnalyzerViews
{
    /// <summary>
    /// MedicalImageAnalysisView - واجهة تحليل الصور الطبية مع أدوات القياس والذكاء الاصطناعي
    /// </summary>
    public partial class MedicalImageAnalysisView : UserControl
    {
        private MedicalImageAnalysisViewModel _viewModel;
        
        // Drawing state
        private bool _isDrawing = false;
        private Point _startPoint;
        private Point _currentPoint;
        private Shape? _currentShape;

        public MedicalImageAnalysisView()
        {
            InitializeComponent();
            InitializeImageAnalysisView();
        }

        /// <summary>
        /// تهيئة واجهة تحليل الصور الطبية
        /// </summary>
        private void InitializeImageAnalysisView()
        {
            try
            {
                // تطبيق ViewModel
                _viewModel = new MedicalImageAnalysisViewModel();
                DataContext = _viewModel;

                // ربط الأحداث
                Loaded += OnImageAnalysisViewLoaded;
                Unloaded += OnImageAnalysisViewUnloaded;

                // ربط أحداث السحب والإفلات
                InitializeDragAndDrop();

                // ربط أحداث ViewModel
                _viewModel.ImageLoadRequested += OnImageLoadRequested;
                _viewModel.AnalysisCompleted += OnAnalysisCompleted;
                _viewModel.ErrorOccurred += OnErrorOccurred;

                // ربط أحداث الماوس للقياسات
                MeasurementCanvas.MouseMove += OnCanvasMouseMove;

                // ربط تغيير الزوم
                ZoomSlider.ValueChanged += OnZoomChanged;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تهيئة واجهة تحليل الصور: {ex.Message}");
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
                MedicalImageDisplay.AllowDrop = true;

                // ربط أحداث السحب والإفلات
                this.DragEnter += OnDragEnter;
                this.DragOver += OnDragOver;
                this.DragLeave += OnDragLeave;
                this.Drop += OnDrop;

                MedicalImageDisplay.DragEnter += OnDragEnter;
                MedicalImageDisplay.Drop += OnDrop;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تهيئة السحب والإفلات: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث تحميل واجهة تحليل الصور
        /// </summary>
        private async void OnImageAnalysisViewLoaded(object sender, RoutedEventArgs e)
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
                ShowErrorMessage($"خطأ في تحميل واجهة تحليل الصور: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث إلغاء تحميل واجهة تحليل الصور
        /// </summary>
        private void OnImageAnalysisViewUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تنظيف الموارد
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إلغاء تحميل واجهة تحليل الصور: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث طلب تحميل الصور
        /// </summary>
        private void OnImageLoadRequested(object? sender, EventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "اختر صورة طبية للتحليل",
                    Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.dcm|" +
                            "صور JPEG|*.jpg;*.jpeg|" +
                            "صور PNG|*.png|" +
                            "صور BMP|*.bmp|" +
                            "صور TIFF|*.tiff|" +
                            "ملفات DICOM|*.dcm|" +
                            "جميع الملفات|*.*",
                    FilterIndex = 1,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    LoadImageFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل الصورة: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل ملف صورة
        /// </summary>
        private async void LoadImageFile(string filePath)
        {
            try
            {
                await _viewModel.LoadImageFileAsync(filePath);
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
                        var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".dcm" };
                        
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
                        LoadImageFile(files[0]);
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
        /// أحداث القياس على الكانفاس
        /// </summary>
        public void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!_viewModel.HasImage) return;

                _startPoint = e.GetPosition(MeasurementCanvas);
                _isDrawing = true;

                // بدء الرسم بناءً على الأداة المختارة
                if (_viewModel.IsRulerToolSelected)
                {
                    StartRulerDrawing();
                }
                else if (_viewModel.IsAreaToolSelected)
                {
                    StartAreaDrawing();
                }
                else if (_viewModel.IsAngleToolSelected)
                {
                    StartAngleDrawing();
                }
                else if (_viewModel.IsCountToolSelected)
                {
                    AddCountMarker();
                }

                MeasurementCanvas.CaptureMouse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في MouseDown: {ex.Message}");
            }
        }

        public void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                _currentPoint = e.GetPosition(MeasurementCanvas);
                
                // تحديث إحداثيات الماوس
                _viewModel.MouseCoordinates = $"({_currentPoint.X:F0}, {_currentPoint.Y:F0})";

                if (_isDrawing && _currentShape != null)
                {
                    UpdateCurrentShape();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في MouseMove: {ex.Message}");
            }
        }

        public void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isDrawing)
                {
                    FinishDrawing();
                    _isDrawing = false;
                    _currentShape = null;
                }

                MeasurementCanvas.ReleaseMouseCapture();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في MouseUp: {ex.Message}");
            }
        }

        /// <summary>
        /// بدء رسم المسطرة
        /// </summary>
        private void StartRulerDrawing()
        {
            var line = new Line
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Stroke = Brushes.Gold,
                StrokeThickness = 2
            };

            _currentShape = line;
            MeasurementCanvas.Children.Add(line);
        }

        /// <summary>
        /// بدء رسم المساحة
        /// </summary>
        private void StartAreaDrawing()
        {
            var rectangle = new Rectangle
            {
                Width = 0,
                Height = 0,
                Stroke = Brushes.LimeGreen,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0))
            };

            Canvas.SetLeft(rectangle, _startPoint.X);
            Canvas.SetTop(rectangle, _startPoint.Y);

            _currentShape = rectangle;
            MeasurementCanvas.Children.Add(rectangle);
        }

        /// <summary>
        /// بدء رسم الزاوية
        /// </summary>
        private void StartAngleDrawing()
        {
            var line = new Line
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };

            _currentShape = line;
            MeasurementCanvas.Children.Add(line);
        }

        /// <summary>
        /// إضافة علامة العدّ
        /// </summary>
        private void AddCountMarker()
        {
            var ellipse = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Red,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            Canvas.SetLeft(ellipse, _startPoint.X - 8);
            Canvas.SetTop(ellipse, _startPoint.Y - 8);

            MeasurementCanvas.Children.Add(ellipse);

            // إضافة إلى العدّاد
            _viewModel.AddCountMarker(_startPoint.X, _startPoint.Y);
        }

        /// <summary>
        /// تحديث الشكل الحالي أثناء الرسم
        /// </summary>
        private void UpdateCurrentShape()
        {
            try
            {
                if (_currentShape is Line line)
                {
                    line.X2 = _currentPoint.X;
                    line.Y2 = _currentPoint.Y;
                }
                else if (_currentShape is Rectangle rectangle)
                {
                    var width = Math.Abs(_currentPoint.X - _startPoint.X);
                    var height = Math.Abs(_currentPoint.Y - _startPoint.Y);
                    
                    rectangle.Width = width;
                    rectangle.Height = height;
                    
                    Canvas.SetLeft(rectangle, Math.Min(_startPoint.X, _currentPoint.X));
                    Canvas.SetTop(rectangle, Math.Min(_startPoint.Y, _currentPoint.Y));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث الشكل: {ex.Message}");
            }
        }

        /// <summary>
        /// إنهاء الرسم وحساب القياسات
        /// </summary>
        private void FinishDrawing()
        {
            try
            {
                if (_currentShape is Line line)
                {
                    var distance = Math.Sqrt(Math.Pow(line.X2 - line.X1, 2) + Math.Pow(line.Y2 - line.Y1, 2));
                    
                    if (_viewModel.IsRulerToolSelected)
                    {
                        _viewModel.AddRulerMeasurement(line.X1, line.Y1, line.X2, line.Y2, distance);
                    }
                    else if (_viewModel.IsAngleToolSelected)
                    {
                        // حساب الزاوية (يتطلب نقاط إضافية)
                        _viewModel.AddAngleMeasurement(line.X1, line.Y1, line.X2, line.Y2);
                    }
                }
                else if (_currentShape is Rectangle rectangle && _viewModel.IsAreaToolSelected)
                {
                    var area = rectangle.Width * rectangle.Height;
                    _viewModel.AddAreaMeasurement(
                        Canvas.GetLeft(rectangle), 
                        Canvas.GetTop(rectangle), 
                        rectangle.Width, 
                        rectangle.Height, 
                        area);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنهاء الرسم: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث تغيير مستوى الزوم
        /// </summary>
        private void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (ImageScrollViewer != null)
                {
                    ImageScrollViewer.ZoomToFactor(e.NewValue);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تغيير الزوم: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث إكمال التحليل
        /// </summary>
        private void OnAnalysisCompleted(object? sender, EventArgs e)
        {
            try
            {
                ShowSuccessMessage("تم إكمال التحليل بنجاح!");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ بعد إكمال التحليل: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث حدوث خطأ
        /// </summary>
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
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف موارد تحليل الصور: {ex.Message}");
            }
        }
    }
}