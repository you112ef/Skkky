using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer.Views
{
    /// <summary>
    /// MainWindow - النافذة الرئيسية لنظام محلل المختبرات الطبية
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel;
        private DispatcherTimer? _timeTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeMainWindow();
        }

        /// <summary>
        /// تهيئة النافذة الرئيسية
        /// </summary>
        private void InitializeMainWindow()
        {
            try
            {
                // تطبيق ViewModel
                _viewModel = new MainWindowViewModel();
                DataContext = _viewModel;

                // ربط الأحداث
                Loaded += OnWindowLoaded;
                Closing += OnWindowClosing;
                
                // ربط أحداث التنقل
                _viewModel.NavigationRequested += OnNavigationRequested;
                _viewModel.DialogRequested += OnDialogRequested;
                _viewModel.StatusUpdated += OnStatusUpdated;

                // ربط حدث النقر على قائمة المستخدم
                UserMenuButton.Click += OnUserMenuClick;

                // تهيئة مؤقت الوقت
                InitializeTimeTimer();

                // تطبيق الإعدادات المحفوظة
                LoadWindowSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تهيئة النافذة الرئيسية: {ex.Message}", "خطأ", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// تهيئة مؤقت الوقت
        /// </summary>
        private void InitializeTimeTimer()
        {
            _timeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeTimer.Tick += (s, e) =>
            {
                _viewModel.CurrentTime = DateTime.Now;
            };
            _timeTimer.Start();
        }

        /// <summary>
        /// تحميل إعدادات النافذة المحفوظة
        /// </summary>
        private void LoadWindowSettings()
        {
            try
            {
                // تحميل حجم وموقع النافذة
                if (Properties.Settings.Default.MainWindowMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else
                {
                    if (Properties.Settings.Default.MainWindowWidth > 0)
                        Width = Properties.Settings.Default.MainWindowWidth;
                    if (Properties.Settings.Default.MainWindowHeight > 0)
                        Height = Properties.Settings.Default.MainWindowHeight;
                    if (Properties.Settings.Default.MainWindowLeft > 0)
                        Left = Properties.Settings.Default.MainWindowLeft;
                    if (Properties.Settings.Default.MainWindowTop > 0)
                        Top = Properties.Settings.Default.MainWindowTop;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل إعدادات النافذة: {ex.Message}");
            }
        }

        /// <summary>
        /// حفظ إعدادات النافذة
        /// </summary>
        private void SaveWindowSettings()
        {
            try
            {
                Properties.Settings.Default.MainWindowMaximized = WindowState == WindowState.Maximized;
                if (WindowState == WindowState.Normal)
                {
                    Properties.Settings.Default.MainWindowWidth = Width;
                    Properties.Settings.Default.MainWindowHeight = Height;
                    Properties.Settings.Default.MainWindowLeft = Left;
                    Properties.Settings.Default.MainWindowTop = Top;
                }
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في حفظ إعدادات النافذة: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث تحميل النافذة
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تهيئة النظام
                await _viewModel.InitializeSystemAsync();

                // الانتقال للصفحة الافتراضية
                NavigateToPage("Dashboard");

                // بدء فحص النظام الدوري
                StartSystemMonitoring();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("خطأ في تحميل النظام", $"حدث خطأ أثناء تحميل النظام: {ex.Message}");
            }
        }

        /// <summary>
        /// بدء مراقبة النظام
        /// </summary>
        private void StartSystemMonitoring()
        {
            var monitoringTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // فحص كل دقيقة
            };
            monitoringTimer.Tick += async (s, e) =>
            {
                await _viewModel.UpdateSystemStatusAsync();
            };
            monitoringTimer.Start();
        }

        /// <summary>
        /// حدث إغلاق النافذة
        /// </summary>
        private async void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // التأكيد من المستخدم
                var result = MessageBox.Show("هل تريد إغلاق النظام؟", "تأكيد الإغلاق", 
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                // حفظ الإعدادات
                SaveWindowSettings();

                // تنظيف الموارد
                await _viewModel.ShutdownSystemAsync();

                // إيقاف المؤقتات
                _timeTimer?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إغلاق النافذة: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث طلب التنقل
        /// </summary>
        private void OnNavigationRequested(object? sender, string pageName)
        {
            NavigateToPage(pageName);
        }

        /// <summary>
        /// حدث طلب حوار
        /// </summary>
        private void OnDialogRequested(object? sender, (string title, string message, MessageBoxImage icon) args)
        {
            MessageBox.Show(args.message, args.title, MessageBoxButton.OK, args.icon);
        }

        /// <summary>
        /// حدث تحديث الحالة
        /// </summary>
        private void OnStatusUpdated(object? sender, string status)
        {
            _viewModel.StatusMessage = status;
        }

        /// <summary>
        /// حدث النقر على قائمة المستخدم
        /// </summary>
        private void OnUserMenuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                button?.ContextMenu?.OpenContextMenu();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح قائمة المستخدم: {ex.Message}");
            }
        }

        /// <summary>
        /// التنقل لصفحة معينة
        /// </summary>
        private void NavigateToPage(string pageName)
        {
            try
            {
                UserControl? page = pageName switch
                {
                    "Dashboard" => new DashboardView(),
                    "Patients" => new PatientManagementView(),
                    "Exams" => new ExamManagementView(),
                    "CASA" => new Views.AnalyzerViews.CASAAnalysisView(),
                    "ImageAnalysis" => new Views.AnalyzerViews.MedicalImageAnalysisView(),
                    "BloodTests" => new Views.AnalyzerViews.BloodTestsView(),
                    "BodyFluidTests" => new Views.AnalyzerViews.BodyFluidTestsView(),
                    "Reports" => new Views.ReportsView(),
                    "Calibration" => new Views.CalibrationView(),
                    "UserManagement" => new Views.UserManagementView(),
                    "SystemSettings" => new Views.SystemSettingsView(),
                    "AIModels" => new Views.AIModelsView(),
                    _ => new DashboardView()
                };

                if (page != null)
                {
                    MainContentFrame.Navigate(page);
                    _viewModel.CurrentPageTitle = GetPageTitle(pageName);

                    // تأثير انتقال المحتوى
                    var fadeIn = FindResource("ContentFadeIn") as System.Windows.Media.Animation.Storyboard;
                    fadeIn?.Begin(MainContentFrame);
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("خطأ في التنقل", $"حدث خطأ أثناء التنقل للصفحة: {ex.Message}");
            }
        }

        /// <summary>
        /// الحصول على عنوان الصفحة
        /// </summary>
        private string GetPageTitle(string pageName)
        {
            return pageName switch
            {
                "Dashboard" => "لوحة التحكم",
                "Patients" => "إدارة المرضى", 
                "Exams" => "إدارة الفحوصات",
                "CASA" => "تحليل الحيوانات المنوية CASA",
                "ImageAnalysis" => "تحليل الصور الطبية",
                "BloodTests" => "فحوصات الدم",
                "BodyFluidTests" => "فحوصات السوائل",
                "Reports" => "التقارير والإحصائيات",
                "Calibration" => "معايرة الأجهزة",
                "UserManagement" => "إدارة المستخدمين",
                "SystemSettings" => "إعدادات النظام",
                "AIModels" => "إدارة نماذج الذكاء الاصطناعي",
                _ => "النظام"
            };
        }

        /// <summary>
        /// عرض حوار خطأ
        /// </summary>
        private void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// تنظيف الموارد
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _timeTimer?.Stop();
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف موارد النافذة الرئيسية: {ex.Message}");
            }
        }
    }
}