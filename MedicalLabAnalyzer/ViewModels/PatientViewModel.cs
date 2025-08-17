using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Services;
using MedicalLabAnalyzer.Models;

namespace MedicalLabAnalyzer.ViewModels
{
    public class PatientViewModel : BaseViewModel, IRefreshableViewModel
    {
        private readonly ILogger<PatientViewModel> _logger;
        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        private readonly MediaService _mediaService;
        
        private ObservableCollection<Patient> _patients;
        private ObservableCollection<Patient> _filteredPatients;
        private Patient? _selectedPatient;
        private bool _isLoading = false;
        private string _searchText = string.Empty;
        private string _statusMessage = string.Empty;
        private User? _currentUser;
        private bool _isEditMode = false;
        private Patient _editingPatient;
        
        public PatientViewModel(
            ILogger<PatientViewModel> logger,
            DatabaseService dbService,
            AuditService auditService,
            MediaService mediaService)
        {
            _logger = logger;
            _dbService = dbService;
            _auditService = auditService;
            _mediaService = mediaService;
            
            _patients = new ObservableCollection<Patient>();
            _filteredPatients = new ObservableCollection<Patient>();
            _editingPatient = new Patient();
            
            // Commands
            LoadPatientsCommand = new AsyncRelayCommand(LoadPatientsAsync);
            SearchCommand = new RelayCommand(PerformSearch);
            AddPatientCommand = new RelayCommand(StartAddPatient);
            EditPatientCommand = new RelayCommand(StartEditPatient, () => SelectedPatient != null);
            DeletePatientCommand = new AsyncRelayCommand(DeletePatientAsync, () => SelectedPatient != null);
            SavePatientCommand = new AsyncRelayCommand(SavePatientAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            ViewPatientDetailsCommand = new RelayCommand(ViewPatientDetails, () => SelectedPatient != null);
            UploadPhotoCommand = new AsyncRelayCommand(UploadPhotoAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        }
        
        #region Properties
        
        public ObservableCollection<Patient> Patients
        {
            get => _patients;
            set => SetProperty(ref _patients, value);
        }
        
        public ObservableCollection<Patient> FilteredPatients
        {
            get => _filteredPatients;
            set => SetProperty(ref _filteredPatients, value);
        }
        
        public Patient? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (SetProperty(ref _selectedPatient, value))
                {
                    OnPropertyChanged(nameof(IsPatientSelected));
                    ((RelayCommand)EditPatientCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeletePatientCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ViewPatientDetailsCommand).RaiseCanExecuteChanged();
                }
            }
        }
        
        public Patient EditingPatient
        {
            get => _editingPatient;
            set => SetProperty(ref _editingPatient, value);
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
        
        public bool IsPatientSelected => SelectedPatient != null;
        
        public bool CanEdit => CurrentUser?.Role != UserRole.Receptionist;
        
        public bool CanDelete => CurrentUser?.Role == UserRole.Manager;
        
        public int TotalPatients => Patients.Count;
        
        public int FilteredCount => FilteredPatients.Count;
        
        #endregion
        
        #region Commands
        
        public ICommand LoadPatientsCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand AddPatientCommand { get; }
        public ICommand EditPatientCommand { get; }
        public ICommand DeletePatientCommand { get; }
        public ICommand SavePatientCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand ViewPatientDetailsCommand { get; }
        public ICommand UploadPhotoCommand { get; }
        public ICommand RefreshCommand { get; }
        
        #endregion
        
        #region Events
        
        public event EventHandler<PatientSelectedEventArgs>? PatientDetailsRequested;
        public event EventHandler<PatientUpdatedEventArgs>? PatientUpdated;
        
        #endregion
        
        #region Methods
        
        public async Task InitializeAsync(User user)
        {
            CurrentUser = user;
            await LoadPatientsAsync();
        }
        
        public async Task RefreshAsync()
        {
            await LoadPatientsAsync();
        }
        
        private async Task LoadPatientsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحميل بيانات المرضى...";
                
                var patients = await _dbService.GetAllPatientsAsync();
                
                Patients.Clear();
                foreach (var patient in patients.OrderByDescending(p => p.CreatedAt))
                {
                    Patients.Add(patient);
                }
                
                PerformSearch(); // تطبيق الفلترة
                
                StatusMessage = $"تم تحميل {patients.Count} مريض";
                
                _logger.LogInformation("تم تحميل {Count} مريض", patients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات المرضى");
                StatusMessage = "خطأ في تحميل البيانات";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void PerformSearch()
        {
            try
            {
                FilteredPatients.Clear();
                
                var searchTerm = SearchText?.Trim().ToLower() ?? string.Empty;
                
                var filtered = string.IsNullOrEmpty(searchTerm)
                    ? Patients
                    : Patients.Where(p => 
                        p.FullName.ToLower().Contains(searchTerm) ||
                        p.PhoneNumber?.ToLower().Contains(searchTerm) == true ||
                        p.NationalId?.ToLower().Contains(searchTerm) == true ||
                        p.MedicalRecordNumber?.ToLower().Contains(searchTerm) == true);
                
                foreach (var patient in filtered)
                {
                    FilteredPatients.Add(patient);
                }
                
                OnPropertyChanged(nameof(FilteredCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في البحث");
            }
        }
        
        private void StartAddPatient()
        {
            EditingPatient = new Patient
            {
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = CurrentUser?.UserId ?? 0
            };
            IsEditMode = true;
            StatusMessage = "إضافة مريض جديد";
        }
        
        private void StartEditPatient()
        {
            if (SelectedPatient == null) return;
            
            EditingPatient = new Patient
            {
                PatientId = SelectedPatient.PatientId,
                FullName = SelectedPatient.FullName,
                DateOfBirth = SelectedPatient.DateOfBirth,
                Gender = SelectedPatient.Gender,
                PhoneNumber = SelectedPatient.PhoneNumber,
                Email = SelectedPatient.Email,
                Address = SelectedPatient.Address,
                NationalId = SelectedPatient.NationalId,
                MedicalRecordNumber = SelectedPatient.MedicalRecordNumber,
                EmergencyContactName = SelectedPatient.EmergencyContactName,
                EmergencyContactPhone = SelectedPatient.EmergencyContactPhone,
                MedicalHistory = SelectedPatient.MedicalHistory,
                Allergies = SelectedPatient.Allergies,
                CurrentMedications = SelectedPatient.CurrentMedications,
                PhotoPath = SelectedPatient.PhotoPath,
                Notes = SelectedPatient.Notes
            };
            
            IsEditMode = true;
            StatusMessage = $"تعديل بيانات المريض: {SelectedPatient.FullName}";
        }
        
        private async Task SavePatientAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري حفظ بيانات المريض...";
                
                // التحقق من صحة البيانات
                if (!ValidatePatient(EditingPatient))
                {
                    StatusMessage = "الرجاء التحقق من البيانات المدخلة";
                    return;
                }
                
                Patient savedPatient;
                
                if (EditingPatient.PatientId == 0)
                {
                    // إضافة مريض جديد
                    EditingPatient.CreatedAt = DateTime.UtcNow;
                    EditingPatient.CreatedByUserId = CurrentUser?.UserId ?? 0;
                    
                    savedPatient = await _dbService.AddPatientAsync(EditingPatient);
                    Patients.Insert(0, savedPatient);
                    
                    await _auditService.LogAsync(
                        $"تم إضافة مريض جديد: {savedPatient.FullName}",
                        AuditActionType.Create,
                        CurrentUser?.UserId,
                        $"Patient added: ID {savedPatient.PatientId}"
                    );
                    
                    StatusMessage = "تم إضافة المريض بنجاح";
                }
                else
                {
                    // تحديث مريض موجود
                    EditingPatient.UpdatedAt = DateTime.UtcNow;
                    EditingPatient.UpdatedByUserId = CurrentUser?.UserId;
                    
                    savedPatient = await _dbService.UpdatePatientAsync(EditingPatient);
                    
                    // تحديث في القائمة
                    var index = Patients.ToList().FindIndex(p => p.PatientId == savedPatient.PatientId);
                    if (index >= 0)
                    {
                        Patients[index] = savedPatient;
                    }
                    
                    await _auditService.LogAsync(
                        $"تم تحديث بيانات المريض: {savedPatient.FullName}",
                        AuditActionType.Update,
                        CurrentUser?.UserId,
                        $"Patient updated: ID {savedPatient.PatientId}"
                    );
                    
                    StatusMessage = "تم تحديث بيانات المريض بنجاح";
                }
                
                PerformSearch(); // إعادة تطبيق الفلترة
                IsEditMode = false;
                SelectedPatient = savedPatient;
                
                PatientUpdated?.Invoke(this, new PatientUpdatedEventArgs(savedPatient));
                
                _logger.LogInformation("تم حفظ بيانات المريض {PatientId}", savedPatient.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حفظ بيانات المريض");
                StatusMessage = "خطأ في حفظ البيانات";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private async Task DeletePatientAsync()
        {
            if (SelectedPatient == null) return;
            
            try
            {
                // تأكيد الحذف
                var patientName = SelectedPatient.FullName;
                var patientId = SelectedPatient.PatientId;
                
                await _dbService.DeletePatientAsync(patientId);
                
                Patients.Remove(SelectedPatient);
                PerformSearch();
                SelectedPatient = null;
                
                await _auditService.LogAsync(
                    $"تم حذف المريض: {patientName}",
                    AuditActionType.Delete,
                    CurrentUser?.UserId,
                    $"Patient deleted: ID {patientId}"
                );
                
                StatusMessage = "تم حذف المريض بنجاح";
                
                _logger.LogInformation("تم حذف المريض {PatientId}", patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف المريض");
                StatusMessage = "خطأ في حذف المريض";
            }
        }
        
        private void CancelEdit()
        {
            IsEditMode = false;
            EditingPatient = new Patient();
            StatusMessage = "تم إلغاء التعديل";
        }
        
        private void ViewPatientDetails()
        {
            if (SelectedPatient != null)
            {
                PatientDetailsRequested?.Invoke(this, new PatientSelectedEventArgs(SelectedPatient));
            }
        }
        
        private async Task UploadPhotoAsync()
        {
            try
            {
                // فتح حوار اختيار الملف
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.gif;*.bmp|جميع الملفات|*.*",
                    Title = "اختر صورة المريض"
                };
                
                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = "جاري رفع الصورة...";
                    
                    // معالجة وحفظ الصورة
                    var photoPath = await _mediaService.ProcessPatientPhotoAsync(
                        openFileDialog.FileName, 
                        EditingPatient.PatientId
                    );
                    
                    EditingPatient.PhotoPath = photoPath;
                    StatusMessage = "تم رفع الصورة بنجاح";
                    
                    _logger.LogInformation("تم رفع صورة للمريض {PatientId}", EditingPatient.PatientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في رفع صورة المريض");
                StatusMessage = "خطأ في رفع الصورة";
            }
        }
        
        private bool ValidatePatient(Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.FullName))
            {
                StatusMessage = "اسم المريض مطلوب";
                return false;
            }
            
            if (patient.DateOfBirth > DateTime.Now)
            {
                StatusMessage = "تاريخ الولادة غير صحيح";
                return false;
            }
            
            if (!string.IsNullOrEmpty(patient.Email) && !IsValidEmail(patient.Email))
            {
                StatusMessage = "البريد الإلكتروني غير صحيح";
                return false;
            }
            
            return true;
        }
        
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
    }
    
    #region Event Args
    
    public class PatientSelectedEventArgs : EventArgs
    {
        public Patient Patient { get; }
        
        public PatientSelectedEventArgs(Patient patient)
        {
            Patient = patient;
        }
    }
    
    public class PatientUpdatedEventArgs : EventArgs
    {
        public Patient Patient { get; }
        
        public PatientUpdatedEventArgs(Patient patient)
        {
            Patient = patient;
        }
    }
    
    #endregion
}