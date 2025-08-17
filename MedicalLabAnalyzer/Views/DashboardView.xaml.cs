using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer.Views
{
    /// <summary>
    /// DashboardView - لوحة التحكم الرئيسية مع دعم الذكاء الاصطناعي
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private DashboardViewModel _viewModel;
        private DispatcherTimer? _refreshTimer;

        public DashboardView()
        {
            InitializeComponent();
            InitializeDashboard();
        }

        /// <summary>
        /// تهيئة لوحة التحكم
        /// </summary>
        private void InitializeDashboard()
        {
            try
            {
                // تطبيق ViewModel
                _viewModel = new DashboardViewModel();
                DataContext = _viewModel;

                // ربط الأحداث
                Loaded += OnDashboardLoaded;
                Unloaded += OnDashboardUnloaded;

                // تهيئة مؤقت التحديث
                InitializeRefreshTimer();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تهيئة لوحة التحكم: {ex.Message}");
            }
        }

        /// <summary>
        /// تهيئة مؤقت التحديث
        /// </summary>
        private void InitializeRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5) // تحديث كل 5 دقائق
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await _viewModel.RefreshDataAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في تحديث البيانات: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// حدث تحميل لوحة التحكم
        /// </summary>
        private async void OnDashboardLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحميل البيانات الأولية
                await _viewModel.LoadDashboardDataAsync();

                // بدء مؤقت التحديث
                _refreshTimer?.Start();

                // تشغيل تأثيرات الدخول
                PlaySlideInAnimations();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل بيانات لوحة التحكم: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث إلغاء تحميل لوحة التحكم
        /// </summary>
        private void OnDashboardUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // إيقاف مؤقت التحديث
                _refreshTimer?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إلغاء تحميل لوحة التحكم: {ex.Message}");
            }
        }

        /// <summary>
        /// تشغيل تأثيرات الدخول
        /// </summary>
        private void PlaySlideInAnimations()
        {
            try
            {
                var slideIn = FindResource("SlideInStoryboard") as System.Windows.Media.Animation.Storyboard;
                slideIn?.Begin(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تشغيل التأثيرات: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث النقر على بطاقة الإحصائيات
        /// </summary>
        private void OnStatCardClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.Tag is string tag)
                {
                    // تحديد الإجراء بناءً على البطاقة المنقورة
                    switch (tag)
                    {
                        case "Patients":
                            _viewModel.NavigateToPatients();
                            break;
                        case "TodayExams":
                            _viewModel.NavigateToTodayExams();
                            break;
                        case "PendingResults":
                            _viewModel.NavigateToPendingResults();
                            break;
                        case "AIAnalyses":
                            _viewModel.NavigateToAIAnalyses();
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine($"Unknown stat card: {tag}");
                            break;
                    }

                    // تأثير النقر
                    PlayClickAnimation(element);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في التنقل: {ex.Message}");
            }
        }

        /// <summary>
        /// تشغيل تأثير النقر
        /// </summary>
        private void PlayClickAnimation(FrameworkElement element)
        {
            try
            {
                // تأثير تصغير وتكبير سريع
                var scaleAnimation = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.95, 
                    TimeSpan.FromMilliseconds(100))
                {
                    AutoReverse = true
                };

                var scaleTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
                element.RenderTransform = scaleTransform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnimation);
                scaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnimation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تأثير النقر: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض رسالة خطأ
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

        /// <summary>
        /// تنظيف الموارد
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _refreshTimer?.Stop();
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف موارد لوحة التحكم: {ex.Message}");
            }
        }
    }
}