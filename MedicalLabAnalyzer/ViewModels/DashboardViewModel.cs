using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.ViewModels
{
    public class DashboardViewModel : BaseViewModel, IRefreshableViewModel
    {
        private readonly ILogger<DashboardViewModel> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        
        private User? _currentUser;
        private DashboardStatistics _statistics;
        private ObservableCollection<RecentExam> _recentExams;
        private ObservableCollection<PendingTask> _pendingTasks;
        private ObservableCollection<SystemAlert> _systemAlerts;
        private bool _isLoading = false;
        private string _statusMessage = string.Empty;
        
        public DashboardViewModel(
            ILogger<DashboardViewModel> logger,
            DatabaseService dbService,
            AuditService auditService)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            
            _statistics = new DashboardStatistics();
            _recentExams = new ObservableCollection<RecentExam>();
            _pendingTasks = new ObservableCollection<PendingTask>();
            _systemAlerts = new ObservableCollection<SystemAlert>();
            
            // Commands
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            ViewExamCommand = new RelayCommand<int>(ViewExam);
            ProcessTaskCommand = new AsyncRelayCommand<int>(ProcessTaskAsync);
            DismissAlertCommand = new RelayCommand<int>(DismissAlert);
        }
        
        #region Properties
        
        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }
        
        public DashboardStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }
        
        public ObservableCollection<RecentExam> RecentExams
        {
            get => _recentExams;
            set => SetProperty(ref _recentExams, value);
        }
        
        public ObservableCollection<PendingTask> PendingTasks
        {
            get => _pendingTasks;
            set => SetProperty(ref _pendingTasks, value);
        }
        
        public ObservableCollection<SystemAlert> SystemAlerts
        {
            get => _systemAlerts;
            set => SetProperty(ref _systemAlerts, value);
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public string WelcomeMessage => $"مرحباً {CurrentUser?.FullName}";
        
        public string TodayDate => DateTime.Now.ToString("dddd، dd MMMM yyyy", new System.Globalization.CultureInfo("ar-SA"));
        
        #endregion
        
        #region Commands
        
        public ICommand RefreshCommand { get; }
        public ICommand ViewExamCommand { get; }
        public ICommand ProcessTaskCommand { get; }
        public ICommand DismissAlertCommand { get; }
        
        #endregion
        
        #region Events
        
        public event EventHandler<ViewExamEventArgs>? ExamViewRequested;
        public event EventHandler<TaskProcessedEventArgs>? TaskProcessed;
        
        #endregion
        
        #region Methods
        
        public async Task InitializeAsync(User user)
        {
            try
            {
                CurrentUser = user;
                StatusMessage = "جاري تحميل لوحة التحكم...";
                
                await RefreshAsync();
                
                _logger.LogInformation("تم تهيئة لوحة التحكم للمستخدم {UserId}", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تهيئة لوحة التحكم");
                StatusMessage = "خطأ في تحميل البيانات";
            }
        }
        
        public async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحديث البيانات...";
                
                // تحميل الإحصائيات
                await LoadStatisticsAsync();
                
                // تحميل الفحوصات الأخيرة
                await LoadRecentExamsAsync();
                
                // تحميل المهام المعلقة
                await LoadPendingTasksAsync();
                
                // تحميل تنبيهات النظام
                await LoadSystemAlertsAsync();
                
                StatusMessage = $"آخر تحديث: {DateTime.Now:HH:mm}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث لوحة التحكم");
                StatusMessage = "خطأ في تحديث البيانات";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task LoadStatisticsAsync()
        {
            try
            {
                var stats = await _dbService.GetDashboardStatisticsAsync(CurrentUser?.UserId);
                
                Statistics = new DashboardStatistics
                {
                    TotalPatientsToday = stats.TotalPatientsToday,
                    TotalExamsToday = stats.TotalExamsToday,
                    PendingExams = stats.PendingExams,
                    CompletedExams = stats.CompletedExams,
                    CriticalResults = stats.CriticalResults,
                    SystemLoad = stats.SystemLoad,
                    
                    // إحصائيات أسبوعية
                    WeeklyPatients = stats.WeeklyPatients,
                    WeeklyExams = stats.WeeklyExams,
                    WeeklyRevenue = stats.WeeklyRevenue,
                    
                    // إحصائيات شهرية
                    MonthlyPatients = stats.MonthlyPatients,
                    MonthlyExams = stats.MonthlyExams,
                    MonthlyRevenue = stats.MonthlyRevenue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الإحصائيات");
                Statistics = new DashboardStatistics(); // إحصائيات فارغة
            }
        }
        
        private async Task LoadRecentExamsAsync()
        {
            try
            {
                var exams = await _dbService.GetRecentExamsAsync(CurrentUser?.UserId, 10);
                
                RecentExams.Clear();
                foreach (var exam in exams)
                {
                    RecentExams.Add(new RecentExam
                    {
                        ExamId = exam.ExamId,
                        PatientName = exam.Patient?.FullName ?? "غير محدد",
                        TestType = exam.TestType.ToString(),
                        Status = exam.Status.ToString(),
                        CreatedAt = exam.CreatedAt,
                        Priority = exam.Priority.ToString(),
                        HasCriticalResults = exam.HasCriticalResults
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الفحوصات الأخيرة");
            }
        }
        
        private async Task LoadPendingTasksAsync()
        {
            try
            {
                var tasks = await _dbService.GetPendingTasksAsync(CurrentUser?.UserId);
                
                PendingTasks.Clear();
                foreach (var task in tasks)
                {
                    PendingTasks.Add(new PendingTask
                    {
                        TaskId = task.TaskId,
                        Title = task.Title,
                        Description = task.Description,
                        Priority = task.Priority,
                        DueDate = task.DueDate,
                        Type = task.Type,
                        RelatedExamId = task.RelatedExamId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل المهام المعلقة");
            }
        }
        
        private async Task LoadSystemAlertsAsync()
        {
            try
            {
                var alerts = await _dbService.GetSystemAlertsAsync(CurrentUser?.UserId);
                
                SystemAlerts.Clear();
                foreach (var alert in alerts)
                {
                    SystemAlerts.Add(new SystemAlert
                    {
                        AlertId = alert.AlertId,
                        Title = alert.Title,
                        Message = alert.Message,
                        Type = alert.Type,
                        Severity = alert.Severity,
                        CreatedAt = alert.CreatedAt,
                        IsRead = alert.IsRead
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تنبيهات النظام");
            }
        }
        
        private void ViewExam(int? examId)
        {
            if (examId.HasValue)
            {
                _logger.LogInformation("طلب عرض الفحص {ExamId}", examId.Value);
                ExamViewRequested?.Invoke(this, new ViewExamEventArgs(examId.Value));
            }
        }
        
        private async Task ProcessTaskAsync(int? taskId)
        {
            if (!taskId.HasValue) return;
            
            try
            {
                _logger.LogInformation("معالجة المهمة {TaskId}", taskId.Value);
                
                // معالجة المهمة
                await _dbService.ProcessTaskAsync(taskId.Value, CurrentUser?.UserId ?? 0);
                
                // إزالة المهمة من القائمة
                var task = PendingTasks.FirstOrDefault(t => t.TaskId == taskId.Value);
                if (task != null)
                {
                    PendingTasks.Remove(task);
                }
                
                TaskProcessed?.Invoke(this, new TaskProcessedEventArgs(taskId.Value));
                
                // تسجيل في سجل المراجعة
                await _auditService.LogAsync(
                    $"تمت معالجة المهمة {taskId.Value}",
                    AuditActionType.TaskProcessed,
                    CurrentUser?.UserId,
                    $"Task {taskId.Value} processed"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في معالجة المهمة {TaskId}", taskId.Value);
                StatusMessage = "خطأ في معالجة المهمة";
            }
        }
        
        private void DismissAlert(int? alertId)
        {
            if (!alertId.HasValue) return;
            
            try
            {
                var alert = SystemAlerts.FirstOrDefault(a => a.AlertId == alertId.Value);
                if (alert != null)
                {
                    SystemAlerts.Remove(alert);
                    
                    // تحديث في قاعدة البيانات
                    _ = _dbService.DismissAlertAsync(alertId.Value, CurrentUser?.UserId ?? 0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إزالة التنبيه {AlertId}", alertId.Value);
            }
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    public class DashboardStatistics
    {
        public int TotalPatientsToday { get; set; }
        public int TotalExamsToday { get; set; }
        public int PendingExams { get; set; }
        public int CompletedExams { get; set; }
        public int CriticalResults { get; set; }
        public double SystemLoad { get; set; }
        
        public int WeeklyPatients { get; set; }
        public int WeeklyExams { get; set; }
        public decimal WeeklyRevenue { get; set; }
        
        public int MonthlyPatients { get; set; }
        public int MonthlyExams { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }
    
    public class RecentExam
    {
        public int ExamId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool HasCriticalResults { get; set; }
        
        public string DisplayText => $"{PatientName} - {TestType}";
        public string TimeAgo => GetTimeAgo(CreatedAt);
        
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1) return "الآن";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} دقيقة";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} ساعة";
            return $"{(int)timeSpan.TotalDays} يوم";
        }
    }
    
    public class PendingTask
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? RelatedExamId { get; set; }
        
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Now;
        public bool IsUrgent => Priority == "عاجل" || Priority == "Critical";
    }
    
    public class SystemAlert
    {
        public int AlertId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        
        public bool IsCritical => Severity == "Critical" || Severity == "خطير";
        public bool IsWarning => Severity == "Warning" || Severity == "تحذير";
    }
    
    #endregion
    
    #region Event Args
    
    public class ViewExamEventArgs : EventArgs
    {
        public int ExamId { get; }
        
        public ViewExamEventArgs(int examId)
        {
            ExamId = examId;
        }
    }
    
    public class TaskProcessedEventArgs : EventArgs
    {
        public int TaskId { get; }
        
        public TaskProcessedEventArgs(int taskId)
        {
            TaskId = taskId;
        }
    }
    
    #endregion
}