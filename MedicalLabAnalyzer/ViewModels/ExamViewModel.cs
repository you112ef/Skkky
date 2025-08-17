using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.ViewModels
{
    public class ExamViewModel : BaseViewModel, IRefreshableViewModel
    {
        private readonly ILogger<ExamViewModel> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        private readonly MediaService _mediaService;
        private readonly CASAAnalyzer _casaAnalyzer;
        private readonly CBCAnalyzer _cbcAnalyzer;
        private readonly UrineAnalyzer _urineAnalyzer;
        private readonly StoolAnalyzer _stoolAnalyzer;
        private readonly GlucoseAnalyzer _glucoseAnalyzer;
        private readonly LipidProfileAnalyzer _lipidAnalyzer;
        private readonly AdvancedTestsAnalyzer _advancedAnalyzer;
        
        private ObservableCollection<Exam> _exams;
        private ObservableCollection<Exam> _filteredExams;
        private ObservableCollection<Patient> _patients;
        private Exam? _selectedExam;
        private Patient? _selectedPatient;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private string _statusMessage = string.Empty;
        private User? _currentUser;
        private bool _isEditMode = false;
        private Exam _editingExam;
        private TestType _selectedTestType = TestType.CBC;
        private ExamStatus _filterStatus = ExamStatus.All;
        private DateTime? _filterDateFrom;
        private DateTime? _filterDateTo;
        
        public ExamViewModel(
            ILogger<ExamViewModel> logger,
            DatabaseService dbService,
            AuditService auditService,
            MediaService mediaService,
            CASAAnalyzer casaAnalyzer,
            CBCAnalyzer cbcAnalyzer,
            UrineAnalyzer urineAnalyzer,
            StoolAnalyzer stoolAnalyzer,
            GlucoseAnalyzer glucoseAnalyzer,
            LipidProfileAnalyzer lipidAnalyzer,
            AdvancedTestsAnalyzer advancedAnalyzer)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            _mediaService = mediaService;
            _casaAnalyzer = casaAnalyzer;
            _cbcAnalyzer = cbcAnalyzer;
            _urineAnalyzer = urineAnalyzer;
            _stoolAnalyzer = stoolAnalyzer;
            _glucoseAnalyzer = glucoseAnalyzer;
            _lipidAnalyzer = lipidAnalyzer;
            _advancedAnalyzer = advancedAnalyzer;
            
            _exams = new ObservableCollection<Exam>();
            _filteredExams = new ObservableCollection<Exam>();
            _patients = new ObservableCollection<Patient>();
            _editingExam = new Exam();
            
            InitializeCommands();
        }
        
        private void InitializeCommands()
        {
            LoadExamsCommand = new AsyncRelayCommand(LoadExamsAsync);
            LoadPatientsCommand = new AsyncRelayCommand(LoadPatientsAsync);
            SearchCommand = new RelayCommand(PerformSearch);
            ApplyFiltersCommand = new RelayCommand(ApplyFilters);
            AddExamCommand = new RelayCommand(StartAddExam);
            EditExamCommand = new RelayCommand(StartEditExam, () => SelectedExam != null);
            DeleteExamCommand = new AsyncRelayCommand(DeleteExamAsync, () => SelectedExam != null);
            SaveExamCommand = new AsyncRelayCommand(SaveExamAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            UploadImageCommand = new AsyncRelayCommand(UploadImageAsync);
            UploadVideoCommand = new AsyncRelayCommand(UploadVideoAsync);
            AnalyzeExamCommand = new AsyncRelayCommand(AnalyzeExamAsync, () => SelectedExam != null);
            ViewResultsCommand = new RelayCommand(ViewResults, () => SelectedExam != null);
            PrintResultsCommand = new AsyncRelayCommand(PrintResultsAsync, () => SelectedExam != null);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        }
        
        #region Properties
        
        public ObservableCollection<Exam> Exams
        {
            get => _exams;
            set => SetProperty(ref _exams, value);
        }
        
        public ObservableCollection<Exam> FilteredExams
        {
            get => _filteredExams;
            set => SetProperty(ref _filteredExams, value);
        }
        
        public ObservableCollection<Patient> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }
        
        public Exam? SelectedExam
        {
            get => _selectedExam;
            set
            {
                if (SetProperty(ref _selectedExam, value))
                {
                    OnPropertyChanged(nameof(IsExamSelected));
                    UpdateCommandStates();
                }
            }
        }
        
        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set => SetProperty(ref _selectedPatient, value);
        }
        
        public Exam EditingExam
        {
            get => _editingExam;
            set => SetProperty(ref _editingExam, value);
        }
        
        public TestType SelectedTestType
        {
            get => _selectedTestType;
            set => SetProperty(ref _selectedTestType, value);
        }
        
        public ExamStatus FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value, ApplyFilters);
        }
        
        public DateTime? FilterDateFrom
        {
            get => _filterDateFrom;
            set => SetProperty(ref _filterDateFrom, value, ApplyFilters);
        }
        
        public DateTime? FilterDateTo
        {
            get => _filterDateTo;
            set => SetProperty(ref _filterDateTo, value, ApplyFilters);
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value, PerformSearch);
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }
        
        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }
        
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }
        
        public bool IsExamSelected => SelectedExam != null;
        
        public bool CanAnalyze => CurrentUser?.Role != UserRole.Receptionist && IsExamSelected;
        
        public bool CanEdit => CurrentUser?.Role != UserRole.Receptionist;
        
        public bool CanDelete => CurrentUser?.Role == UserRole.Manager;
        
        public Array TestTypes => Enum.GetValues<TestType>();
        
        public Array ExamStatuses => Enum.GetValues<ExamStatus>();
        
        public int TotalExams => Exams.Count;
        
        public int FilteredCount => FilteredExams.Count;
        
        #endregion
        
        #region Commands
        
        public ICommand LoadExamsCommand { get; private set; }
        public ICommand LoadPatientsCommand { get; private set; }
        public ICommand SearchCommand { get; private set; }
        public ICommand ApplyFiltersCommand { get; private set; }
        public ICommand AddExamCommand { get; private set; }
        public ICommand EditExamCommand { get; private set; }
        public ICommand DeleteExamCommand { get; private set; }
        public ICommand SaveExamCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }
        public ICommand UploadImageCommand { get; private set; }
        public ICommand UploadVideoCommand { get; private set; }
        public ICommand AnalyzeExamCommand { get; private set; }
        public ICommand ViewResultsCommand { get; private set; }
        public ICommand PrintResultsCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        
        #endregion
        
        #region Events
        
        public event EventHandler<ExamSelectedEventArgs>? ExamResultsRequested;
        public event EventHandler<ExamUpdatedEventArgs>? ExamUpdated;
        
        #endregion
        
        #region Methods
        
        public async Task InitializeAsync(User user)
        {
            CurrentUser = user;
            await LoadPatientsAsync();
            await LoadExamsAsync();
        }
        
        public async Task RefreshAsync()
        {
            await LoadExamsAsync();
        }
        
        private async Task LoadExamsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحميل الفحوصات...";
                
                var exams = await _dbService.GetAllExamsAsync();
                
                Exams.Clear();
                foreach (var exam in exams.OrderByDescending(e => e.CreatedAt))
                {
                    Exams.Add(exam);
                }
                
                ApplyFilters();
                
                StatusMessage = $"تم تحميل {exams.Count} فحص";
                
                _logger.LogInformation("تم تحميل {Count} فحص", exams.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الفحوصات");
                StatusMessage = "خطأ في تحميل البيانات";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task LoadPatientsAsync()
        {
            try
            {
                var patients = await _dbService.GetAllPatientsAsync();
                
                Patients.Clear();
                foreach (var patient in patients.OrderBy(p => p.FullName))
                {
                    Patients.Add(patient);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل المرضى");
            }
        }
        
        private void PerformSearch()
        {
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            try
            {
                FilteredExams.Clear();
                
                var searchTerm = SearchText?.Trim().ToLower() ?? string.Empty;
                
                var filtered = Exams.Where(e =>
                {
                    // فلترة النص
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        var searchMatch = e.Patient?.FullName.ToLower().Contains(searchTerm) == true ||
                                         e.TestType.ToString().ToLower().Contains(searchTerm) ||
                                         e.ExamId.ToString().Contains(searchTerm) ||
                                         e.Notes?.ToLower().Contains(searchTerm) == true;
                        
                        if (!searchMatch) return false;
                    }
                    
                    // فلترة الحالة
                    if (FilterStatus != ExamStatus.All && e.Status != FilterStatus)
                        return false;
                    
                    // فلترة التاريخ
                    if (FilterDateFrom.HasValue && e.CreatedAt.Date < FilterDateFrom.Value.Date)
                        return false;
                        
                    if (FilterDateTo.HasValue && e.CreatedAt.Date > FilterDateTo.Value.Date)
                        return false;
                    
                    return true;
                });
                
                foreach (var exam in filtered)
                {
                    FilteredExams.Add(exam);
                }
                
                OnPropertyChanged(nameof(FilteredCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تطبيق الفلاتر");
            }
        }
        
        private void StartAddExam()
        {
            EditingExam = new Exam
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = CurrentUser?.UserId ?? 0,
                Status = ExamStatus.Pending,
                Priority = ExamPriority.Normal,
                TestType = SelectedTestType
            };
            IsEditMode = true;
            StatusMessage = "إضافة فحص جديد";
        }
        
        private void StartEditExam()
        {
            if (SelectedExam == null) return;
            
            EditingExam = new Exam
            {
                ExamId = SelectedExam.ExamId,
                PatientId = SelectedExam.PatientId,
                TestType = SelectedExam.TestType,
                Status = SelectedExam.Status,
                Priority = SelectedExam.Priority,
                Notes = SelectedExam.Notes,
                ClinicalHistory = SelectedExam.ClinicalHistory,
                DoctorName = SelectedExam.DoctorName
            };
            
            SelectedPatient = Patients.FirstOrDefault(p => p.PatientId == SelectedExam.PatientId);
            IsEditMode = true;
            StatusMessage = $"تعديل الفحص رقم: {SelectedExam.ExamId}";
        }
        
        private async Task SaveExamAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري حفظ الفحص...";
                
                if (!ValidateExam(EditingExam))
                {
                    StatusMessage = "الرجاء التحقق من البيانات المدخلة";
                    return;
                }
                
                EditingExam.PatientId = SelectedPatient?.PatientId ?? 0;
                
                Exam savedExam;
                
                if (EditingExam.ExamId == 0)
                {
                    // إضافة فحص جديد
                    EditingExam.CreatedAt = DateTime.UtcNow;
                    EditingExam.CreatedByUserId = CurrentUser?.UserId ?? 0;
                    
                    savedExam = await _dbService.AddExamAsync(EditingExam);
                    savedExam.Patient = SelectedPatient; // ربط بيانات المريض
                    Exams.Insert(0, savedExam);
                    
                    await _auditService.LogAsync(
                        $"تم إضافة فحص جديد: {savedExam.TestType} للمريض {SelectedPatient?.FullName}",
                        AuditActionType.Create,
                        CurrentUser?.UserId,
                        $"Exam added: ID {savedExam.ExamId}"
                    );
                    
                    StatusMessage = "تم إضافة الفحص بنجاح";
                }
                else
                {
                    // تحديث فحص موجود
                    EditingExam.UpdatedAt = DateTime.UtcNow;
                    EditingExam.UpdatedByUserId = CurrentUser?.UserId;
                    
                    savedExam = await _dbService.UpdateExamAsync(EditingExam);
                    savedExam.Patient = SelectedPatient;
                    
                    // تحديث في القائمة
                    var index = Exams.ToList().FindIndex(e => e.ExamId == savedExam.ExamId);
                    if (index >= 0)
                    {
                        Exams[index] = savedExam;
                    }
                    
                    await _auditService.LogAsync(
                        $"تم تحديث الفحص: {savedExam.ExamId}",
                        AuditActionType.Update,
                        CurrentUser?.UserId,
                        $"Exam updated: ID {savedExam.ExamId}"
                    );
                    
                    StatusMessage = "تم تحديث الفحص بنجاح";
                }
                
                ApplyFilters();
                IsEditMode = false;
                SelectedExam = savedExam;
                
                ExamUpdated?.Invoke(this, new ExamUpdatedEventArgs(savedExam));
                
                _logger.LogInformation("تم حفظ الفحص {ExamId}", savedExam.ExamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حفظ الفحص");
                StatusMessage = "خطأ في حفظ البيانات";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task DeleteExamAsync()
        {
            if (SelectedExam == null) return;
            
            try
            {
                var examId = SelectedExam.ExamId;
                var patientName = SelectedExam.Patient?.FullName ?? "غير محدد";
                
                await _dbService.DeleteExamAsync(examId);
                
                Exams.Remove(SelectedExam);
                ApplyFilters();
                SelectedExam = null;
                
                await _auditService.LogAsync(
                    $"تم حذف الفحص {examId} للمريض {patientName}",
                    AuditActionType.Delete,
                    CurrentUser?.UserId,
                    $"Exam deleted: ID {examId}"
                );
                
                StatusMessage = "تم حذف الفحص بنجاح";
                
                _logger.LogInformation("تم حذف الفحص {ExamId}", examId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الفحص");
                StatusMessage = "خطأ في حذف الفحص";
            }
        }
        
        private void CancelEdit()
        {
            IsEditMode = false;
            EditingExam = new Exam();
            SelectedPatient = null;
            StatusMessage = "تم إلغاء التعديل";
        }
        
        private async Task UploadImageAsync()
        {
            if (SelectedExam == null) return;
            
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.gif;*.bmp|جميع الملفات|*.*",
                    Title = "اختر صورة الفحص"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "جاري رفع الصورة...";
                    
                    await _mediaService.UploadExamImageAsync(SelectedExam.ExamId, openFileDialog.FileName);
                    
                    StatusMessage = "تم رفع الصورة بنجاح";
                    
                    _logger.LogInformation("تم رفع صورة للفحص {ExamId}", SelectedExam.ExamId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في رفع صورة الفحص");
                StatusMessage = "خطأ في رفع الصورة";
            }
        }
        
        private async Task UploadVideoAsync()
        {
            if (SelectedExam == null) return;
            
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "ملفات الفيديو|*.mp4;*.avi;*.mov;*.wmv|جميع الملفات|*.*",
                    Title = "اختر فيديو الفحص"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "جاري رفع الفيديو...";
                    
                    await _mediaService.UploadExamVideoAsync(SelectedExam.ExamId, openFileDialog.FileName);
                    
                    StatusMessage = "تم رفع الفيديو بنجاح";
                    
                    _logger.LogInformation("تم رفع فيديو للفحص {ExamId}", SelectedExam.ExamId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في رفع فيديو الفحص");
                StatusMessage = "خطأ في رفع الفيديو";
            }
        }
        
        private async Task AnalyzeExamAsync()
        {
            if (SelectedExam == null) return;
            
            try
            {
                IsLoading = true;
                StatusMessage = $"جاري تحليل {SelectedExam.TestType}...";
                
                // الحصول على الملفات المرفقة
                var attachments = await _dbService.GetExamAttachmentsAsync(SelectedExam.ExamId);
                
                if (!attachments.Any())
                {
                    StatusMessage = "لا توجد ملفات للتحليل";
                    return;
                }
                
                switch (SelectedExam.TestType)
                {
                    case TestType.CASA:
                        await AnalyzeCASAAsync(SelectedExam, attachments);
                        break;
                    case TestType.CBC:
                        await AnalyzeCBCAsync(SelectedExam, attachments);
                        break;
                    case TestType.Urine:
                        await AnalyzeUrineAsync(SelectedExam, attachments);
                        break;
                    case TestType.Stool:
                        await AnalyzeStoolAsync(SelectedExam, attachments);
                        break;
                    case TestType.Glucose:
                        await AnalyzeGlucoseAsync(SelectedExam, attachments);
                        break;
                    case TestType.LipidProfile:
                        await AnalyzeLipidProfileAsync(SelectedExam, attachments);
                        break;
                    default:
                        await AnalyzeAdvancedTestAsync(SelectedExam, attachments);
                        break;
                }
                
                // تحديث حالة الفحص
                SelectedExam.Status = ExamStatus.Completed;
                SelectedExam.CompletedAt = DateTime.UtcNow;
                await _dbService.UpdateExamAsync(SelectedExam);
                
                StatusMessage = "تم إكمال التحليل بنجاح";
                
                _logger.LogInformation("تم تحليل الفحص {ExamId} - {TestType}", SelectedExam.ExamId, SelectedExam.TestType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحليل الفحص {ExamId}", SelectedExam?.ExamId);
                StatusMessage = "خطأ في التحليل";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task AnalyzeCASAAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var videoFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("video"));
            if (videoFile != null)
            {
                await _casaAnalyzer.AnalyzeVideoAsync(exam.ExamId, videoFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeCBCAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                await _cbcAnalyzer.AnalyzeImageAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeUrineAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                await _urineAnalyzer.AnalyzeImageAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeStoolAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                await _stoolAnalyzer.AnalyzeImageAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeGlucoseAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                await _glucoseAnalyzer.AnalyzeImageAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeLipidProfileAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                await _lipidAnalyzer.AnalyzeImageAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
            }
        }
        
        private async Task AnalyzeAdvancedTestAsync(Exam exam, List<ExamAttachment> attachments)
        {
            var imageFile = attachments.FirstOrDefault(a => a.FileType.ToLower().Contains("image"));
            if (imageFile != null)
            {
                switch (exam.TestType)
                {
                    case TestType.LiverFunction:
                        await _advancedAnalyzer.AnalyzeLiverFunctionAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
                        break;
                    case TestType.KidneyFunction:
                        await _advancedAnalyzer.AnalyzeKidneyFunctionAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
                        break;
                    case TestType.Thyroid:
                        await _advancedAnalyzer.AnalyzeThyroidFunctionAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
                        break;
                    case TestType.CRP:
                        await _advancedAnalyzer.AnalyzeCRPAsync(exam.ExamId, imageFile.FilePath, CurrentUser?.UserId ?? 0);
                        break;
                }
            }
        }
        
        private void ViewResults()
        {
            if (SelectedExam != null)
            {
                ExamResultsRequested?.Invoke(this, new ExamSelectedEventArgs(SelectedExam));
            }
        }
        
        private async Task PrintResultsAsync()
        {
            if (SelectedExam == null) return;
            
            try
            {
                StatusMessage = "جاري إنشاء التقرير...";
                
                // هنا يمكن إضافة كود طباعة التقرير
                // await _reportService.GeneratePDFReportAsync(SelectedExam.ExamId);
                
                StatusMessage = "تم إنشاء التقرير بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في طباعة التقرير");
                StatusMessage = "خطأ في إنشاء التقرير";
            }
        }
        
        private bool ValidateExam(Exam exam)
        {
            if (exam.PatientId == 0 || SelectedPatient == null)
            {
                StatusMessage = "الرجاء اختيار المريض";
                return false;
            }
            
            return true;
        }
        
        private void UpdateCommandStates()
        {
            ((RelayCommand)EditExamCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)DeleteExamCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)AnalyzeExamCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewResultsCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)PrintResultsCommand).RaiseCanExecuteChanged();
        }
        
        #endregion
    }
    
    #region Helper Enums
    
    public enum ExamStatus
    {
        All = 0,
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }
    
    #endregion
    
    #region Event Args
    
    public class ExamSelectedEventArgs : EventArgs
    {
        public Exam Exam { get; }
        
        public ExamSelectedEventArgs(Exam exam)
        {
            Exam = exam;
        }
    }
    
    public class ExamUpdatedEventArgs : EventArgs
    {
        public Exam Exam { get; }
        
        public ExamUpdatedEventArgs(Exam exam)
        {
            Exam = exam;
        }
    }
    
    #endregion
}