using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MedicalLabAnalyzer.ViewModels;

namespace MedicalLabAnalyzer.Views
{
    /// <summary>
    /// PatientManagementView - واجهة إدارة المرضى مع البحث والتصفية
    /// </summary>
    public partial class PatientManagementView : UserControl
    {
        private PatientViewModel _viewModel;

        public PatientManagementView()
        {
            InitializeComponent();
            InitializePatientManagementView();
        }

        /// <summary>
        /// تهيئة واجهة إدارة المرضى
        /// </summary>
        private void InitializePatientManagementView()
        {
            try
            {
                // تطبيق ViewModel
                _viewModel = new PatientViewModel();
                DataContext = _viewModel;

                // ربط الأحداث
                Loaded += OnPatientManagementViewLoaded;
                Unloaded += OnPatientManagementViewUnloaded;

                // ربط أحداث ViewModel
                _viewModel.PatientSelected += OnPatientSelected;
                _viewModel.ErrorOccurred += OnErrorOccurred;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تهيئة واجهة إدارة المرضى: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث تحميل واجهة إدارة المرضى
        /// </summary>
        private async void OnPatientManagementViewLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحميل البيانات الأولية
                await _viewModel.LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل بيانات المرضى: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث إلغاء تحميل واجهة إدارة المرضى
        /// </summary>
        private void OnPatientManagementViewUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // تنظيف الموارد
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إلغاء تحميل واجهة إدارة المرضى: {ex.Message}");
            }
        }

        /// <summary>
        /// حدث النقر على بطاقة المريض
        /// </summary>
        private void OnPatientCardClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.Tag is string patientId)
                {
                    _viewModel.SelectPatient(patientId);
                    
                    // تأثير النقر
                    PlayClickAnimation(element);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحديد المريض: {ex.Message}");
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
                var scaleAnimation = new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.98, 
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
        /// حدث تحديد مريض
        /// </summary>
        private void OnPatientSelected(object? sender, EventArgs e)
        {
            try
            {
                // يمكن إضافة منطق إضافي عند تحديد مريض
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في حدث تحديد المريض: {ex.Message}");
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
                _viewModel?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تنظيف موارد إدارة المرضى: {ex.Message}");
            }
        }
    }
}