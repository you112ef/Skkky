using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer.Views
{
    /// <summary>
    /// LoginView - واجهة تسجيل الدخول مع دعم الذكاء الاصطناعي والواجهة العربية
    /// </summary>
    public partial class LoginView : UserControl
    {
        private LoginViewModel _viewModel;
        private Storyboard? _shakeAnimation;

        public LoginView()
        {
            InitializeComponent();
            InitializeView();
        }

        /// <summary>
        /// تهيئة الواجهة والأحداث
        /// </summary>
        private void InitializeView()
        {
            // تطبيق ViewModel
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            // ربط الأحداث
            Loaded += OnViewLoaded;
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.LoginFailed += OnLoginFailed;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // ربط أحداث الإدخال
            UsernameTextBox.KeyDown += OnTextBoxKeyDown;
            PasswordBox.KeyDown += OnPasswordBoxKeyDown;

            // تركيز على حقل اسم المستخدم
            Loaded += (s, e) => UsernameTextBox.Focus();

            // تهيئة الرسوم المتحركة
            _shakeAnimation = FindResource("ShakeAnimation") as Storyboard;
        }

        /// <summary>
        /// حدث تحميل الواجهة
        /// </summary>
        private async void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // فحص حالة النظام
                await _viewModel.CheckSystemStatusAsync();

                // تحميل إعدادات تذكر المستخدم
                LoadRememberedUser();

                // تفعيل تأثيرات الدخول
                PlayFadeInAnimation();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل الواجهة: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل بيانات المستخدم المحفوظة
        /// </summary>
        private void LoadRememberedUser()
        {
            try
            {
                var rememberedUsername = Properties.Settings.Default.RememberedUsername;
                if (!string.IsNullOrEmpty(rememberedUsername))
                {
                    _viewModel.Username = rememberedUsername;
                    _viewModel.RememberMe = true;
                    PasswordBox.Focus(); // تركيز على كلمة المرور إذا كان اسم المستخدم محفوظ
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل المستخدم المحفوظ: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث تغيير خصائص ViewModel
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.HasError) && _viewModel.HasError)
            {
                // تشغيل تأثير الاهتزاز للأخطاء
                PlayShakeAnimation();
            }
        }

        /// <summary>
        /// حدث نجاح تسجيل الدخول
        /// </summary>
        private async void OnLoginSuccessful(object? sender, EventArgs e)
        {
            try
            {
                // حفظ بيانات تذكر المستخدم
                if (_viewModel.RememberMe)
                {
                    Properties.Settings.Default.RememberedUsername = _viewModel.Username;
                }
                else
                {
                    Properties.Settings.Default.RememberedUsername = string.Empty;
                }
                Properties.Settings.Default.Save();

                // تأثير الخروج
                await PlayFadeOutAnimation();

                // الانتقال للواجهة الرئيسية
                NavigateToMainWindow();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ بعد تسجيل الدخول: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث فشل تسجيل الدخول
        /// </summary>
        private void OnLoginFailed(object? sender, string errorMessage)
        {
            ShowErrorMessage(errorMessage);
            PlayShakeAnimation();
            
            // محو كلمة المرور وتركيز عليها
            PasswordBox.Clear();
            PasswordBox.Focus();
        }

        /// <summary>
        /// التعامل مع ضغط المفاتيح في حقل النص
        /// </summary>
        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                UsernameTextBox.Clear();
                e.Handled = true;
            }
        }

        /// <summary>
        /// التعامل مع ضغط المفاتيح في حقل كلمة المرور
        /// </summary>
        private void OnPasswordBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.LoginCommand.CanExecute(null))
                {
                    _viewModel.LoginCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PasswordBox.Clear();
                UsernameTextBox.Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// تشغيل تأثير الدخول
        /// </summary>
        private void PlayFadeInAnimation()
        {
            try
            {
                var fadeIn = FindResource("FadeInAnimation") as Storyboard;
                fadeIn?.Begin(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تأثير الدخول: {ex.Message}");
            }
        }

        /// <summary>
        /// تشغيل تأثير الخروج
        /// </summary>
        private System.Threading.Tasks.Task PlayFadeOutAnimation()
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                        BeginAnimation(OpacityProperty, fadeOut);
                    });
                    
                    System.Threading.Thread.Sleep(300);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"خطأ في تأثير الخروج: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// تشغيل تأثير الاهتزاز
        /// </summary>
        private void PlayShakeAnimation()
        {
            try
            {
                _shakeAnimation?.Begin(LoginButton);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تأثير الاهتزاز: {ex.Message}");
            }
        }

        /// <summary>
        /// عرض رسالة خطأ
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            _viewModel.ErrorMessage = message;
            _viewModel.HasError = true;

            // إخفاء الرسالة تلقائياً بعد 5 ثوان
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (s, e) =>
            {
                _viewModel.HasError = false;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// الانتقال للنافذة الرئيسية
        /// </summary>
        private void NavigateToMainWindow()
        {
            try
            {
                // البحث عن النافذة الرئيسية
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // إذا كانت النافذة الرئيسية موجودة، تحديث المحتوى
                    if (mainWindow.Content is Frame frame)
                    {
                        frame.Navigate(new DashboardView());
                    }
                    else
                    {
                        // إنشاء نافذة رئيسية جديدة
                        var newMainWindow = new MainWindow();
                        newMainWindow.Show();
                        mainWindow.Close();
                    }
                }
                else
                {
                    // إنشاء النافذة الرئيسية
                    var mainWindowInstance = new MainWindow();
                    Application.Current.MainWindow = mainWindowInstance;
                    mainWindowInstance.Show();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في الانتقال للواجهة الرئيسية: {ex.Message}");
            }
        }

        /// <summary>
        /// تنظيف الموارد
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _viewModel.LoginSuccessful -= OnLoginSuccessful;
                _viewModel.LoginFailed -= OnLoginFailed;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف الموارد: {ex.Message}");
            }
        }
    }
}