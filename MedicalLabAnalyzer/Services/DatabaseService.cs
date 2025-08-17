using Microsoft.EntityFrameworkCore;
using MedicalLabAnalyzer.Models;
using System.Globalization;

namespace MedicalLabAnalyzer.Services
{
    public class DatabaseService : DbContext
    {
        // DbSets - Database Tables
        public DbSet&lt;User&gt; Users { get; set; }
        public DbSet&lt;Patient&gt; Patients { get; set; }
        public DbSet&lt;Exam&gt; Exams { get; set; }
        public DbSet&lt;PatientAttachment&gt; PatientAttachments { get; set; }
        public DbSet&lt;ExamAttachment&gt; ExamAttachments { get; set; }
        public DbSet&lt;AuditLog&gt; AuditLogs { get; set; }
        
        // Test Results
        public DbSet&lt;CASAResult&gt; CASAResults { get; set; }
        public DbSet&lt;CBCTestResult&gt; CBCResults { get; set; }
        public DbSet&lt;UrineTestResult&gt; UrineResults { get; set; }
        public DbSet&lt;StoolTestResult&gt; StoolResults { get; set; }
        public DbSet&lt;GlucoseTestResult&gt; GlucoseResults { get; set; }
        public DbSet&lt;LipidProfileTestResult&gt; LipidProfileResults { get; set; }
        public DbSet&lt;LiverFunctionTestResult&gt; LiverFunctionResults { get; set; }
        public DbSet&lt;KidneyFunctionTestResult&gt; KidneyFunctionResults { get; set; }
        public DbSet&lt;CRPTestResult&gt; CRPResults { get; set; }
        public DbSet&lt;ThyroidTestResult&gt; ThyroidResults { get; set; }
        public DbSet&lt;ElectrolytesTestResult&gt; ElectrolytesResults { get; set; }
        public DbSet&lt;CoagulationTestResult&gt; CoagulationResults { get; set; }
        public DbSet&lt;VitaminTestResult&gt; VitaminResults { get; set; }
        public DbSet&lt;HormoneTestResult&gt; HormoneResults { get; set; }
        public DbSet&lt;MicrobiologyTestResult&gt; MicrobiologyResults { get; set; }
        public DbSet&lt;PCRTestResult&gt; PCRResults { get; set; }
        public DbSet&lt;SerologyTestResult&gt; SerologyResults { get; set; }
        
        private readonly string _connectionString;
        
        public DatabaseService()
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "medical_lab.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _connectionString = $"Data Source={dbPath}";
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableDetailedErrors(true);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            ConfigureUserModel(modelBuilder);
            ConfigurePatientModel(modelBuilder);
            ConfigureExamModel(modelBuilder);
            ConfigureAttachmentModels(modelBuilder);
            ConfigureTestResultModels(modelBuilder);
            ConfigureAuditLogModel(modelBuilder);
            
            // Add Indexes for Performance
            AddIndexes(modelBuilder);
            
            // Seed Initial Data
            SeedData(modelBuilder);
        }
        
        private static void ConfigureUserModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity&lt;User&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; e.Username).IsUnique();
                entity.HasIndex(e =&gt; e.Email);
                entity.Property(e =&gt; e.Role).HasConversion&lt;int&gt;();
            });
        }
        
        private static void ConfigurePatientModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity&lt;Patient&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; e.NationalId);
                entity.HasIndex(e =&gt; e.PhoneNumber);
                entity.HasIndex(e =&gt; new { e.FullName, e.DateOfBirth });
                entity.Property(e =&gt; e.Gender).HasConversion&lt;int&gt;();
                entity.Ignore(e =&gt; e.BMI);
                entity.Ignore(e =&gt; e.Age);
            });
        }
        
        private static void ConfigureExamModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity&lt;Exam&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; e.ExamNumber).IsUnique();
                entity.HasIndex(e =&gt; e.PatientId);
                entity.HasIndex(e =&gt; new { e.ExamDate, e.Status });
                entity.Property(e =&gt; e.ExamType).HasConversion&lt;int&gt;();
                entity.Property(e =&gt; e.Status).HasConversion&lt;int&gt;();
                
                // Configure relationships
                entity.HasOne(e =&gt; e.Patient)
                      .WithMany(p =&gt; p.Exams)
                      .HasForeignKey(e =&gt; e.PatientId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e =&gt; e.CreatedByUser)
                      .WithMany(u =&gt; u.ExamsCreated)
                      .HasForeignKey(e =&gt; e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
                      
                entity.HasOne(e =&gt; e.ReviewedByUser)
                      .WithMany(u =&gt; u.ExamsReviewed)
                      .HasForeignKey(e =&gt; e.ReviewedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
        
        private static void ConfigureAttachmentModels(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity&lt;PatientAttachment&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; new { e.PatientId, e.IsDeleted });
                entity.Property(e =&gt; e.Category).HasConversion&lt;int&gt;();
            });
            
            modelBuilder.Entity&lt;ExamAttachment&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; new { e.ExamId, e.IsDeleted });
                entity.Property(e =&gt; e.Category).HasConversion&lt;int&gt;();
            });
        }
        
        private static void ConfigureTestResultModels(ModelBuilder modelBuilder)
        {
            // CASA Result
            modelBuilder.Entity&lt;CASAResult&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; e.ExamId).IsUnique();
                entity.Property(e =&gt; e.Interpretation).HasConversion&lt;int&gt;();
                entity.HasOne(c =&gt; c.Exam)
                      .WithOne(e =&gt; e.CASAResult)
                      .HasForeignKey&lt;CASAResult&gt;(c =&gt; c.ExamId);
            });
            
            // CBC Result
            modelBuilder.Entity&lt;CBCTestResult&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; e.ExamId).IsUnique();
                entity.Property(e =&gt; e.Interpretation).HasConversion&lt;int&gt;();
                entity.HasOne(c =&gt; c.Exam)
                      .WithOne(e =&gt; e.CBCResult)
                      .HasForeignKey&lt;CBCTestResult&gt;(c =&gt; c.ExamId);
            });
            
            // Configure other test results similarly
            ConfigureTestResult&lt;UrineTestResult&gt;(modelBuilder, "UrineResult");
            ConfigureTestResult&lt;StoolTestResult&gt;(modelBuilder, "StoolResult");
            ConfigureTestResult&lt;GlucoseTestResult&gt;(modelBuilder, "GlucoseResult");
            ConfigureTestResult&lt;LipidProfileTestResult&gt;(modelBuilder, "LipidProfileResult");
            ConfigureTestResult&lt;LiverFunctionTestResult&gt;(modelBuilder, "LiverFunctionResult");
            ConfigureTestResult&lt;KidneyFunctionTestResult&gt;(modelBuilder, "KidneyFunctionResult");
            ConfigureTestResult&lt;CRPTestResult&gt;(modelBuilder, "CRPResult");
            ConfigureTestResult&lt;ThyroidTestResult&gt;(modelBuilder, "ThyroidResult");
            ConfigureTestResult&lt;ElectrolytesTestResult&gt;(modelBuilder, "ElectrolytesResult");
            ConfigureTestResult&lt;CoagulationTestResult&gt;(modelBuilder, "CoagulationResult");
            ConfigureTestResult&lt;VitaminTestResult&gt;(modelBuilder, "VitaminResult");
            ConfigureTestResult&lt;HormoneTestResult&gt;(modelBuilder, "HormoneResult");
            ConfigureTestResult&lt;MicrobiologyTestResult&gt;(modelBuilder, "MicrobiologyResult");
            ConfigureTestResult&lt;PCRTestResult&gt;(modelBuilder, "PCRResult");
            ConfigureTestResult&lt;SerologyTestResult&gt;(modelBuilder, "SerologyResult");
        }
        
        private static void ConfigureTestResult&lt;T&gt;(ModelBuilder modelBuilder, string navigationProperty) where T : class
        {
            modelBuilder.Entity&lt;T&gt;(entity =&gt;
            {
                var examIdProperty = typeof(T).GetProperty("ExamId");
                if (examIdProperty != null)
                {
                    entity.HasIndex("ExamId").IsUnique();
                }
                
                // Configure enum conversion for Interpretation property if exists
                var interpretationProperty = typeof(T).GetProperty("Interpretation");
                if (interpretationProperty != null && interpretationProperty.PropertyType.IsEnum)
                {
                    entity.Property("Interpretation").HasConversion&lt;int&gt;();
                }
            });
        }
        
        private static void ConfigureAuditLogModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity&lt;AuditLog&gt;(entity =&gt;
            {
                entity.HasIndex(e =&gt; new { e.TableName, e.RecordId });
                entity.HasIndex(e =&gt; e.Timestamp);
                entity.HasIndex(e =&gt; e.UserId);
                entity.Property(e =&gt; e.Action).HasConversion&lt;int&gt;();
                entity.Property(e =&gt; e.Severity).HasConversion&lt;int&gt;();
            });
        }
        
        private static void AddIndexes(ModelBuilder modelBuilder)
        {
            // Add composite indexes for better query performance
            modelBuilder.Entity&lt;Exam&gt;().HasIndex(e =&gt; new { e.PatientId, e.ExamType, e.Status });
            modelBuilder.Entity&lt;Patient&gt;().HasIndex(e =&gt; new { e.IsActive, e.CreatedAt });
            modelBuilder.Entity&lt;AuditLog&gt;().HasIndex(e =&gt; new { e.UserId, e.Timestamp, e.Action });
        }
        
        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Create default admin user
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin");
            var labPasswordHash = BCrypt.Net.BCrypt.HashPassword("123");
            var receptionPasswordHash = BCrypt.Net.BCrypt.HashPassword("123");
            
            modelBuilder.Entity&lt;User&gt;().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = adminPasswordHash,
                    FullName = "مدير النظام",
                    Email = "admin@medicallab.com",
                    Role = UserRole.Manager,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 2,
                    Username = "lab",
                    PasswordHash = labPasswordHash,
                    FullName = "فني المختبر",
                    Email = "lab@medicallab.com",
                    Role = UserRole.LabTechnician,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    UserId = 3,
                    Username = "reception",
                    PasswordHash = receptionPasswordHash,
                    FullName = "موظف الاستقبال",
                    Email = "reception@medicallab.com",
                    Role = UserRole.Receptionist,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
        
        public void InitializeDatabase()
        {
            try
            {
                // Ensure database is created
                Database.EnsureCreated();
                
                // Apply any pending migrations
                if (Database.GetPendingMigrations().Any())
                {
                    Database.Migrate();
                }
                
                // Create backup directory
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Backup");
                Directory.CreateDirectory(backupDir);
                
                Console.WriteLine("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database initialization error: {ex.Message}");
                throw;
            }
        }
        
        public async Task&lt;bool&gt; BackupDatabase()
        {
            try
            {
                var sourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "medical_lab.db");
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "Backup");
                var backupFile = Path.Combine(backupDir, $"medical_lab_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, backupFile, true);
                    
                    // Keep only last 10 backups
                    var backupFiles = Directory.GetFiles(backupDir, "medical_lab_backup_*.db")
                        .OrderByDescending(f =&gt; File.GetCreationTime(f))
                        .Skip(10);
                    
                    foreach (var oldBackup in backupFiles)
                    {
                        File.Delete(oldBackup);
                    }
                    
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task&lt;bool&gt; RestoreDatabase(string backupFilePath)
        {
            try
            {
                var targetFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "medical_lab.db");
                
                if (File.Exists(backupFilePath))
                {
                    // Close all connections first
                    await Database.CloseConnectionAsync();
                    
                    File.Copy(backupFilePath, targetFile, true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Perform backup before disposing
                _ = Task.Run(() =&gt; BackupDatabase());
            }
            base.Dispose(disposing);
        }
    }
}