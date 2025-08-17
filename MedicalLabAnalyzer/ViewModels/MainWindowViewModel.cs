using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AuditService _auditService;
        
        private User? _currentUser;
        private BaseViewModel? _currentView;
        private string _statusMessage = "جاري التحضير...";
        private bool _isLoading = false;
        private ObservableCollection<string> _recentActivities;
        private ObservableCollection<NavigationItem> _navigationItems;
        
        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider,
            AuditService auditService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _auditService = auditService;
            
            _recentActivities = new ObservableCollection<string>();
            _navigationItems = new ObservableCollection<NavigationItem>();
            
            // Commands
            LogoutCommand = new RelayCommand(Logout);
            NavigateCommand = new RelayCommand<string>(Navigate);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            
            InitializeNavigation();
        }
        
        #region Properties
        
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    UpdateNavigationItems();
                    OnPropertyChanged(nameof(IsUserLoggedIn));
                    OnPropertyChanged(nameof(UserDisplayName));
                    OnPropertyChanged(nameof(UserRole));
                }
            }
        }
        
        public BaseViewModel? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        public bool IsUserLoggedIn => CurrentUser != null;
        
        public string UserDisplayName => CurrentUser?.FullName ?? "غير مسجل";
        
        public string UserRole => CurrentUser?.Role?.ToString() ?? "";
        
        public ObservableCollection<string> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }
        
        public ObservableCollection<NavigationItem> NavigationItems
        {
            get => _navigationItems;
            set => SetProperty(ref _navigationItems, value);
        }
        
        public string WindowTitle => $"محلل المختبرات الطبية - {UserDisplayName}";
        
        #endregion
        
        #region Commands
        
        public ICommand LogoutCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand RefreshCommand { get; }
        
        #endregion
        
        #region Events
        
        public event EventHandler? UserLoggedOut;
        public event EventHandler<NavigationEventArgs>? NavigationRequested;
        
        #endregion
        
        #region Methods
        
        public async Task InitializeAsync(User user)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحضير الواجهة...";
                
                CurrentUser = user;
                
                await LoadRecentActivitiesAsync();
                await LoadDashboardAsync();
                
                StatusMessage = $"مرحباً {user.FullName}";
                
                _logger.LogInformation("تم تهيئة الواجهة الرئيسية للمستخدم {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تهيئة الواجهة الرئيسية");
                StatusMessage = "خطأ في تحضير الواجهة";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void InitializeNavigation()
        {
            NavigationItems.Clear();
            
            // إضافة العناصر الأساسية
            NavigationItems.Add(new NavigationItem("Dashboard", "لوحة التحكم", "🏠", UserRole.All));
            NavigationItems.Add(new NavigationItem("Patients", "إدارة المرضى", "👥", UserRole.All));
            NavigationItems.Add(new NavigationItem("Exams", "إدارة الفحوصات", "🔬", UserRole.All));
            NavigationItems.Add(new NavigationItem("CASA", "تحليل CASA", "🧬", UserRole.LabTechnician | UserRole.Manager));
            NavigationItems.Add(new NavigationItem("Reports", "التقارير", "📊", UserRole.All));
            NavigationItems.Add(new NavigationItem("Calibration", "المعايرة", "⚙️", UserRole.LabTechnician | UserRole.Manager));
            NavigationItems.Add(new NavigationItem("Users", "إدارة المستخدمين", "👤", UserRole.Manager));
            NavigationItems.Add(new NavigationItem("Audit", "سجل المراجعة", "📋", UserRole.Manager));
            NavigationItems.Add(new NavigationItem("Settings", "الإعدادات", "⚙️", UserRole.Manager));
        }
        
        private void UpdateNavigationItems()
        {
            if (CurrentUser == null) return;
            
            var userRole = CurrentUser.Role;
            
            foreach (var item in NavigationItems)
            {
                item.IsVisible = item.RequiredRole.HasFlag(userRole) || item.RequiredRole == UserRole.All;
            }
        }
        
        private async Task LoadRecentActivitiesAsync()
        {
            try
            {
                if (CurrentUser == null) return;
                
                var activities = await _auditService.GetRecentActivitiesAsync(CurrentUser.UserId, 10);
                
                RecentActivities.Clear();
                foreach (var activity in activities)
                {
                    RecentActivities.Add($"{activity.Timestamp:HH:mm} - {activity.Action}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الأنشطة الأخيرة");
            }
        }
        
        private async Task LoadDashboardAsync()
        {
            try
            {
                // تحميل لوحة التحكم كعرض افتراضي
                var dashboardViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
                await dashboardViewModel.InitializeAsync(CurrentUser!);
                CurrentView = dashboardViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل لوحة التحكم");
            }
        }
        
        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري التحديث...";
                
                await LoadRecentActivitiesAsync();
                
                // تحديث العرض الحالي إذا كان يدعم التحديث
                if (CurrentView is IRefreshableViewModel refreshable)
                {
                    await refreshable.RefreshAsync();
                }
                
                StatusMessage = "تم التحديث بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث البيانات");
                StatusMessage = "خطأ في التحديث";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void Navigate(string? destination)
        {
            if (string.IsNullOrEmpty(destination) || CurrentUser == null)
                return;
                
            try
            {
                _logger.LogInformation("التنقل إلى: {Destination}", destination);
                
                NavigationRequested?.Invoke(this, new NavigationEventArgs(destination));
                
                // تسجيل التنقل في سجل المراجعة
                _ = _auditService.LogAsync(
                    $"تم التنقل إلى {destination}",
                    AuditActionType.Navigation,
                    CurrentUser.UserId,
                    $"Navigation to {destination}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التنقل إلى {Destination}", destination);
                StatusMessage = "خطأ في التنقل";
            }
        }
        
        private void Logout()
        {
            try
            {
                if (CurrentUser != null)
                {
                    _logger.LogInformation("تسجيل خروج للمستخدم {UserId}", CurrentUser.UserId);
                    
                    _ = _auditService.LogAsync(
                        $"تم تسجيل خروج المستخدم {CurrentUser.FullName}",
                        AuditActionType.Logout,
                        CurrentUser.UserId,
                        "User logged out"
                    );
                }
                
                CurrentUser = null;
                CurrentView = null;
                RecentActivities.Clear();
                StatusMessage = "تم تسجيل الخروج";
                
                UserLoggedOut?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل الخروج");
                StatusMessage = "خطأ في تسجيل الخروج";
            }
        }
        
        #endregion
    }
    
    public class NavigationItem : BaseViewModel
    {
        private bool _isVisible = true;
        
        public string Key { get; }
        public string Title { get; }
        public string Icon { get; }
        public UserRole RequiredRole { get; }
        
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }
        
        public NavigationItem(string key, string title, string icon, UserRole requiredRole)
        {
            Key = key;
            Title = title;
            Icon = icon;
            RequiredRole = requiredRole;
        }
    }
    
    public class NavigationEventArgs : EventArgs
    {
        public string Destination { get; }
        
        public NavigationEventArgs(string destination)
        {
            Destination = destination;
        }
    }
    
    // Interface for ViewModels that support refresh
    public interface IRefreshableViewModel
    {
        Task RefreshAsync();
    }
}