using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MedicalLabAnalyzer.Models;
using System.Text.Json;

namespace MedicalLabAnalyzer.Services
{
    public class AuditService
    {
        private readonly DatabaseService _dbService;
        private readonly ILogger&lt;AuditService&gt; _logger;
        private readonly Queue&lt;AuditLog&gt; _auditQueue;
        private readonly Timer _auditTimer;
        private readonly object _queueLock = new object();
        
        public AuditService(DatabaseService dbService, ILogger&lt;AuditService&gt; logger)
        {
            _dbService = dbService;
            _logger = logger;
            _auditQueue = new Queue&lt;AuditLog&gt;();
            
            // Process audit logs every 30 seconds
            _auditTimer = new Timer(ProcessAuditQueue, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        
        public async Task LogAsync(
            string tableName,
            int recordId,
            AuditAction action,
            int userId,
            object? oldValues = null,
            object? newValues = null,
            string? notes = null,
            int? patientId = null,
            int? examId = null,
            string? moduleName = null,
            bool isCritical = false,
            string? criticalReason = null,
            AuditSeverity severity = AuditSeverity.Info)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    TableName = tableName,
                    RecordId = recordId,
                    Action = action,
                    UserId = userId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                    Notes = notes,
                    PatientId = patientId,
                    ExamId = examId,
                    ModuleName = moduleName,
                    IsCritical = isCritical,
                    CriticalReason = criticalReason,
                    Severity = severity,
                    Timestamp = DateTime.UtcNow,
                    IPAddress = GetClientIPAddress(),
                    UserAgent = GetUserAgent()
                };
                
                // Add to queue for batch processing
                lock (_queueLock)
                {
                    _auditQueue.Enqueue(auditLog);
                }
                
                // If critical, process immediately
                if (isCritical)
                {
                    await ProcessSingleAuditLog(auditLog);
                }
                
                _logger.LogInformation($"Audit log queued: {action} on {tableName} by user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create audit log for {tableName}:{recordId}");
            }
        }
        
        public async Task LogUserLoginAsync(int userId, bool success, string? failureReason = null)
        {
            await LogAsync(
                tableName: "Users",
                recordId: userId,
                action: AuditAction.Login,
                userId: userId,
                notes: success ? "تسجيل دخول ناجح" : $"فشل تسجيل الدخول: {failureReason}",
                severity: success ? AuditSeverity.Info : AuditSeverity.Warning
            );
        }
        
        public async Task LogUserLogoutAsync(int userId)
        {
            await LogAsync(
                tableName: "Users",
                recordId: userId,
                action: AuditAction.Logout,
                userId: userId,
                notes: "تسجيل خروج"
            );
        }
        
        public async Task LogPatientOperationAsync(int patientId, AuditAction action, int userId, Patient? oldPatient = null, Patient? newPatient = null)
        {
            await LogAsync(
                tableName: "Patients",
                recordId: patientId,
                action: action,
                userId: userId,
                oldValues: oldPatient,
                newValues: newPatient,
                patientId: patientId,
                moduleName: "Patient Management"
            );
        }
        
        public async Task LogExamOperationAsync(int examId, AuditAction action, int userId, Exam? oldExam = null, Exam? newExam = null, int? patientId = null)
        {
            await LogAsync(
                tableName: "Exams",
                recordId: examId,
                action: action,
                userId: userId,
                oldValues: oldExam,
                newValues: newExam,
                patientId: patientId,
                examId: examId,
                moduleName: "Exam Management"
            );
        }
        
        public async Task LogTestResultAsync&lt;T&gt;(int examId, string testType, AuditAction action, int userId, T? oldResult = null, T? newResult = null, int? patientId = null) where T : class
        {
            await LogAsync(
                tableName: typeof(T).Name,
                recordId: examId,
                action: action,
                userId: userId,
                oldValues: oldResult,
                newValues: newResult,
                patientId: patientId,
                examId: examId,
                moduleName: testType
            );
        }
        
        public async Task LogFileOperationAsync(int recordId, string fileName, AuditAction action, int userId, string? filePath = null, int? patientId = null, int? examId = null)
        {
            await LogAsync(
                tableName: "Attachments",
                recordId: recordId,
                action: action,
                userId: userId,
                notes: $"File: {fileName}, Path: {filePath}",
                patientId: patientId,
                examId: examId,
                moduleName: "File Management"
            );
        }
        
        public async Task LogCriticalOperationAsync(string tableName, int recordId, AuditAction action, int userId, string reason, object? details = null)
        {
            await LogAsync(
                tableName: tableName,
                recordId: recordId,
                action: action,
                userId: userId,
                newValues: details,
                isCritical: true,
                criticalReason: reason,
                severity: AuditSeverity.Critical
            );
        }
        
        public async Task LogReportGenerationAsync(int? patientId, int? examId, string reportType, int userId)
        {
            await LogAsync(
                tableName: "Reports",
                recordId: examId ?? patientId ?? 0,
                action: AuditAction.Export,
                userId: userId,
                notes: $"Generated {reportType} report",
                patientId: patientId,
                examId: examId,
                moduleName: "Reports"
            );
        }
        
        public async Task LogCalibrationAsync(string equipmentName, int userId, object calibrationData)
        {
            await LogAsync(
                tableName: "Calibration",
                recordId: 0,
                action: AuditAction.Calibrate,
                userId: userId,
                newValues: calibrationData,
                notes: $"Equipment calibration: {equipmentName}",
                moduleName: "Calibration",
                isCritical: true,
                criticalReason: "Equipment calibration affects test accuracy"
            );
        }
        
        public async Task&lt;List&lt;AuditLog&gt;&gt; GetAuditLogsAsync(
            int? userId = null,
            int? patientId = null,
            int? examId = null,
            AuditAction? action = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? moduleName = null,
            int pageNumber = 1,
            int pageSize = 50)
        {
            try
            {
                using var db = new DatabaseService();
                var query = db.AuditLogs.Include(a =&gt; a.User).AsQueryable();
                
                if (userId.HasValue)
                    query = query.Where(a =&gt; a.UserId == userId.Value);
                    
                if (patientId.HasValue)
                    query = query.Where(a =&gt; a.PatientId == patientId.Value);
                    
                if (examId.HasValue)
                    query = query.Where(a =&gt; a.ExamId == examId.Value);
                    
                if (action.HasValue)
                    query = query.Where(a =&gt; a.Action == action.Value);
                    
                if (fromDate.HasValue)
                    query = query.Where(a =&gt; a.Timestamp &gt;= fromDate.Value);
                    
                if (toDate.HasValue)
                    query = query.Where(a =&gt; a.Timestamp &lt;= toDate.Value);
                    
                if (!string.IsNullOrEmpty(moduleName))
                    query = query.Where(a =&gt; a.ModuleName == moduleName);
                
                return await query
                    .OrderByDescending(a =&gt; a.Timestamp)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve audit logs");
                return new List&lt;AuditLog&gt;();
            }
        }
        
        public async Task&lt;List&lt;AuditLog&gt;&gt; GetCriticalAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                using var db = new DatabaseService();
                var query = db.AuditLogs
                    .Include(a =&gt; a.User)
                    .Where(a =&gt; a.IsCritical)
                    .AsQueryable();
                    
                if (fromDate.HasValue)
                    query = query.Where(a =&gt; a.Timestamp &gt;= fromDate.Value);
                    
                if (toDate.HasValue)
                    query = query.Where(a =&gt; a.Timestamp &lt;= toDate.Value);
                
                return await query
                    .OrderByDescending(a =&gt; a.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve critical audit logs");
                return new List&lt;AuditLog&gt;();
            }
        }
        
        public async Task&lt;Dictionary&lt;string, int&gt;&gt; GetAuditStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var db = new DatabaseService();
                var stats = await db.AuditLogs
                    .Where(a =&gt; a.Timestamp &gt;= fromDate && a.Timestamp &lt;= toDate)
                    .GroupBy(a =&gt; a.Action)
                    .Select(g =&gt; new { Action = g.Key.ToString(), Count = g.Count() })
                    .ToDictionaryAsync(x =&gt; x.Action, x =&gt; x.Count);
                    
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve audit statistics");
                return new Dictionary&lt;string, int&gt;();
            }
        }
        
        private async void ProcessAuditQueue(object? state)
        {
            var logsToProcess = new List&lt;AuditLog&gt;();
            
            lock (_queueLock)
            {
                while (_auditQueue.Count &gt; 0)
                {
                    logsToProcess.Add(_auditQueue.Dequeue());
                }
            }
            
            if (logsToProcess.Count &gt; 0)
            {
                await ProcessAuditLogs(logsToProcess);
            }
        }
        
        private async Task ProcessAuditLogs(List&lt;AuditLog&gt; auditLogs)
        {
            try
            {
                using var db = new DatabaseService();
                await db.AuditLogs.AddRangeAsync(auditLogs);
                await db.SaveChangesAsync();
                
                _logger.LogInformation($"Processed {auditLogs.Count} audit logs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process {auditLogs.Count} audit logs");
                
                // Re-queue failed logs
                lock (_queueLock)
                {
                    foreach (var log in auditLogs)
                    {
                        _auditQueue.Enqueue(log);
                    }
                }
            }
        }
        
        private async Task ProcessSingleAuditLog(AuditLog auditLog)
        {
            try
            {
                using var db = new DatabaseService();
                await db.AuditLogs.AddAsync(auditLog);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process critical audit log immediately");
                
                // Re-queue for batch processing
                lock (_queueLock)
                {
                    _auditQueue.Enqueue(auditLog);
                }
            }
        }
        
        private static string? GetClientIPAddress()
        {
            // For desktop application, return local machine IP
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                var ipAddresses = System.Net.Dns.GetHostAddresses(hostName);
                return ipAddresses.FirstOrDefault(ip =&gt; ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            }
            catch
            {
                return "127.0.0.1";
            }
        }
        
        private static string? GetUserAgent()
        {
            // For desktop application, return application info
            return $"MedicalLabAnalyzer/1.0.0 (Windows NT; {Environment.OSVersion.VersionString})";
        }
        
        public void Dispose()
        {
            _auditTimer?.Dispose();
            
            // Process any remaining logs
            ProcessAuditQueue(null);
        }
    }
}