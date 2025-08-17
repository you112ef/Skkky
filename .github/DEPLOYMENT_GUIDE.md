# دليل النشر - Windows Server 2025 | Deployment Guide

دليل شامل لنشر Medical Lab Analyzer على Windows Server 2025

---

## 📋 جدول المحتويات | Table of Contents

### العربية
1. [نظرة عامة](#نظرة-عامة)
2. [متطلبات النشر](#متطلبات-النشر)
3. [التحضير للنشر](#التحضير-للنشر)
4. [عملية النشر](#عملية-النشر)
5. [التكوين البيئي](#التكوين-البيئي)
6. [المراقبة والصيانة](#المراقبة-والصيانة)
7. [استكشاف الأخطاء](#استكشاف-الأخطاء)

### English
1. [Overview](#overview)
2. [Deployment Requirements](#deployment-requirements)
3. [Pre-Deployment Setup](#pre-deployment-setup)
4. [Deployment Process](#deployment-process)
5. [Environment Configuration](#environment-configuration)
6. [Monitoring & Maintenance](#monitoring--maintenance)
7. [Troubleshooting](#troubleshooting)

---

## نظرة عامة

### 🎯 الهدف
نشر نظام Medical Lab Analyzer بنجاح على Windows Server 2025 مع ضمان:
- الاستقرار والأداء العالي
- الأمان والحماية
- المراقبة المستمرة
- سهولة الصيانة

### 📊 مخطط النشر
```
Developer Machine → GitHub Actions → Windows Server 2025
      ↓                    ↓                ↓
   Build Tests         CI/CD Pipeline    Production App
   Local Tests        Automated Tests   Live Monitoring
```

---

## متطلبات النشر

### 🖥️ متطلبات الخادم
| المكون | المتطلب الأدنى | المستحسن | ملاحظات |
|--------|-------------|---------|---------|
| **نظام التشغيل** | Windows Server 2025 Standard | Windows Server 2025 Datacenter | 64-bit |
| **المعالج** | 4 Cores, 2.4GHz | 8 Cores, 3.0GHz+ | Intel/AMD x64 |
| **الذاكرة** | 8 GB RAM | 16 GB RAM+ | للذكاء الاصطناعي |
| **التخزين** | 100 GB SSD | 500 GB NVMe SSD | أداء عالي |
| **الشبكة** | 1 Gbps | 10 Gbps | للتطبيقات السحابية |

### 🔧 البرامج المطلوبة
```powershell
# .NET 8.0 Runtime
winget install Microsoft.DotNet.Runtime.8

# IIS (اختياري للويب API)
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole

# PowerShell 7.x
winget install Microsoft.PowerShell

# Visual C++ Redistributables
winget install Microsoft.VCRedist.2015+.x64
```

### ☁️ خدمات Azure (اختياري)
- Azure Functions
- Azure Storage Account
- Azure Application Insights
- Azure Key Vault

---

## التحضير للنشر

### 🔐 إعداد الأمان
```powershell
# إنشاء مستخدم النظام
$securePassword = ConvertTo-SecureString "MedLabAnalyzer2025!" -AsPlainText -Force
New-LocalUser -Name "MedLabService" -Password $securePassword -Description "Medical Lab Analyzer Service Account"

# إعداد الصلاحيات
Add-LocalGroupMember -Group "Users" -Member "MedLabService"
Add-LocalGroupMember -Group "IIS_IUSRS" -Member "MedLabService"
```

### 📁 إعداد المجلدات
```powershell
# إنشاء بنية المجلدات
$appPath = "C:\MedicalLabAnalyzer"
$logPath = "C:\MedicalLabAnalyzer\Logs"
$dataPath = "C:\MedicalLabAnalyzer\Data"
$backupPath = "C:\MedicalLabAnalyzer\Backups"

New-Item -Path $appPath, $logPath, $dataPath, $backupPath -ItemType Directory -Force

# تعيين الصلاحيات
icacls $appPath /grant "MedLabService:(OI)(CI)F" /T
```

### 🌐 إعداد Firewall
```powershell
# فتح المنافذ المطلوبة
New-NetFirewallRule -DisplayName "Medical Lab Analyzer HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "Medical Lab Analyzer HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
New-NetFirewallRule -DisplayName "Medical Lab Analyzer App" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow
```

---

## عملية النشر

### 🚀 النشر التلقائي (GitHub Actions)
```yaml
# استخدام workflow المُعد مسبقاً
name: Deploy to Windows Server 2025
on:
  push:
    branches: [main]
    
jobs:
  deploy:
    runs-on: windows-2025
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Deploy to Server
        run: |
          .\.github\scripts\deploy-to-server.ps1
```

### 📦 النشر اليدوي
```powershell
# 1. استنساخ المشروع
git clone https://github.com/you112ef/Skkky.git
cd Skkky\MedicalLabAnalyzer

# 2. البناء للنشر
dotnet publish -c Release -r win-x64 --self-contained true -o "C:\MedicalLabAnalyzer\App"

# 3. نسخ الملفات
Copy-Item -Path ".\publish\*" -Destination "C:\MedicalLabAnalyzer\App\" -Recurse -Force

# 4. تثبيت كخدمة Windows
New-Service -Name "MedicalLabAnalyzer" -BinaryPathName "C:\MedicalLabAnalyzer\App\MedicalLabAnalyzer.exe" -StartupType Automatic

# 5. بدء الخدمة
Start-Service -Name "MedicalLabAnalyzer"
```

### ⚙️ إعداد قاعدة البيانات
```powershell
# إعداد SQLite
$dbPath = "C:\MedicalLabAnalyzer\Data\medicallab.db"
$connectionString = "Data Source=$dbPath;Version=3;"

# تشغيل migrations
dotnet ef database update --connection "$connectionString"

# إعداد البيانات الأولية
.\Scripts\InitializeDatabase.ps1 -ConnectionString $connectionString
```

---

## التكوين البيئي

### 📝 ملف التكوين (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Path": "C:\\MedicalLabAnalyzer\\Logs\\app.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "Database": {
    "ConnectionString": "Data Source=C:\\MedicalLabAnalyzer\\Data\\medicallab.db;Version=3;",
    "BackupInterval": "24:00:00",
    "BackupPath": "C:\\MedicalLabAnalyzer\\Backups\\"
  },
  "AI": {
    "ModelsPath": "C:\\MedicalLabAnalyzer\\Models\\",
    "EnableGPU": true,
    "MaxMemoryUsage": "4GB"
  },
  "Security": {
    "EncryptionKey": "YOUR_ENCRYPTION_KEY_HERE",
    "TokenExpiry": "24:00:00",
    "EnableAuditLog": true
  },
  "Performance": {
    "MaxConcurrentUsers": 50,
    "CacheSize": "256MB",
    "EnablePerformanceCounters": true
  }
}
```

### 🔧 متغيرات البيئة
```powershell
# إعداد متغيرات النظام
[Environment]::SetEnvironmentVariable("MEDLAB_HOME", "C:\MedicalLabAnalyzer", "Machine")
[Environment]::SetEnvironmentVariable("MEDLAB_ENV", "Production", "Machine")
[Environment]::SetEnvironmentVariable("MEDLAB_LOG_LEVEL", "Information", "Machine")

# إعادة تحميل المتغيرات
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine")
```

### 🔒 إعداد SSL/TLS
```powershell
# تثبيت شهادة SSL
$cert = New-SelfSignedCertificate -DnsName "medicallab.local" -CertStoreLocation "cert:\LocalMachine\My"

# ربط الشهادة بالتطبيق
netsh http add sslcert ipport=0.0.0.0:443 certhash=$($cert.Thumbprint) appid='{12345678-1234-1234-1234-123456789012}'
```

---

## المراقبة والصيانة

### 📊 إعداد Performance Counters
```powershell
# إنشاء performance counters مخصصة
$categoryName = "Medical Lab Analyzer"
$counters = @(
    @{Name="Active Users"; Type="NumberOfItems32"},
    @{Name="Database Queries/sec"; Type="RateOfCountsPerSecond32"},
    @{Name="AI Processing Time"; Type="AverageTimer32"},
    @{Name="Memory Usage (MB)"; Type="NumberOfItems32"}
)

# تسجيل الـ counters
foreach ($counter in $counters) {
    New-PerformanceCounterCategory -CategoryName $categoryName -CounterName $counter.Name -CounterType $counter.Type
}
```

### 🔍 نظام المراقبة
```powershell
# سكربت مراقبة مُتخصص
# File: Monitor-MedicalLabAnalyzer.ps1

param(
    [int]$CheckInterval = 300,  # 5 دقائق
    [string]$AlertEmail = "admin@medicallab.com"
)

while ($true) {
    # فحص صحة الخدمة
    $service = Get-Service -Name "MedicalLabAnalyzer" -ErrorAction SilentlyContinue
    
    if ($service.Status -ne "Running") {
        Send-MailMessage -To $AlertEmail -Subject "تحذير: خدمة المختبر متوقفة" -Body "الخدمة متوقفة منذ $(Get-Date)"
        Start-Service -Name "MedicalLabAnalyzer"
    }
    
    # فحص استهلاك الذاكرة
    $process = Get-Process -Name "MedicalLabAnalyzer" -ErrorAction SilentlyContinue
    if ($process -and $process.WorkingSet64 -gt 2GB) {
        Write-Warning "استهلاك ذاكرة عالي: $([math]::Round($process.WorkingSet64/1GB, 2)) GB"
    }
    
    # فحص مساحة القرص
    $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DriveType=3"
    $freeSpace = ($disk.FreeSpace / $disk.Size) * 100
    
    if ($freeSpace -lt 20) {
        Send-MailMessage -To $AlertEmail -Subject "تحذير: مساحة القرص منخفضة" -Body "المساحة المتبقية: $([math]::Round($freeSpace, 2))%"
    }
    
    Start-Sleep -Seconds $CheckInterval
}
```

### 📈 Dashboard المراقبة
```html
<!DOCTYPE html>
<html dir="rtl" lang="ar">
<head>
    <title>مراقبة Medical Lab Analyzer</title>
    <meta charset="utf-8">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <h1>لوحة مراقبة النظام</h1>
    
    <div class="metrics">
        <div class="metric">
            <h3>حالة الخدمة</h3>
            <span id="service-status">جاري التحقق...</span>
        </div>
        
        <div class="metric">
            <h3>المستخدمون النشطون</h3>
            <span id="active-users">0</span>
        </div>
        
        <div class="metric">
            <h3>استهلاك الذاكرة</h3>
            <canvas id="memory-chart"></canvas>
        </div>
        
        <div class="metric">
            <h3>أداء قاعدة البيانات</h3>
            <canvas id="db-performance-chart"></canvas>
        </div>
    </div>
    
    <script>
        // تحديث البيانات كل 30 ثانية
        setInterval(updateMetrics, 30000);
        
        function updateMetrics() {
            fetch('/api/metrics')
                .then(response => response.json())
                .then(data => {
                    document.getElementById('service-status').textContent = 
                        data.serviceRunning ? 'يعمل بشكل طبيعي' : 'متوقف';
                    document.getElementById('active-users').textContent = data.activeUsers;
                });
        }
    </script>
</body>
</html>
```

---

## استكشاف الأخطاء

### 🚨 المشاكل الشائعة والحلول

#### 1. مشكلة عدم بدء الخدمة
```powershell
# التشخيص
Get-EventLog -LogName Application -Source "Medical Lab Analyzer" -Newest 10

# الحلول المحتملة
# أ. فحص الصلاحيات
icacls "C:\MedicalLabAnalyzer" /verify

# ب. فحص التبعيات
Get-Service -Name "MedicalLabAnalyzer" | Select-Object -ExpandProperty RequiredServices

# ج. إعادة تثبيت الخدمة
sc delete "MedicalLabAnalyzer"
New-Service -Name "MedicalLabAnalyzer" -BinaryPathName "C:\MedicalLabAnalyzer\App\MedicalLabAnalyzer.exe"
```

#### 2. مشكلة الاتصال بقاعدة البيانات
```powershell
# فحص ملف قاعدة البيانات
Test-Path "C:\MedicalLabAnalyzer\Data\medicallab.db"

# فحص الصلاحيات
icacls "C:\MedicalLabAnalyzer\Data\medicallab.db"

# استعادة من النسخة الاحتياطية
$latestBackup = Get-ChildItem "C:\MedicalLabAnalyzer\Backups\" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Copy-Item $latestBackup.FullName "C:\MedicalLabAnalyzer\Data\medicallab.db" -Force
```

#### 3. مشكلة أداء الذكاء الاصطناعي
```powershell
# فحص ذاكرة كارت الرسوميات
nvidia-smi

# فحص نماذج الذكاء الاصطناعي
Test-Path "C:\MedicalLabAnalyzer\Models\yolov8.onnx"
Test-Path "C:\MedicalLabAnalyzer\Models\deepsort.onnx"

# إعادة تحميل النماذج
Invoke-RestMethod -Uri "https://models.medicallab.com/download/latest" -OutFile "C:\MedicalLabAnalyzer\Models\models.zip"
Expand-Archive "C:\MedicalLabAnalyzer\Models\models.zip" -DestinationPath "C:\MedicalLabAnalyzer\Models\" -Force
```

### 📋 Log Analysis
```powershell
# تحليل الـ logs
$logPath = "C:\MedicalLabAnalyzer\Logs\"
$errors = Get-ChildItem $logPath -Filter "*.log" | 
    ForEach-Object { Get-Content $_.FullName | Where-Object { $_ -match "ERROR|FATAL" } }

$errors | Group-Object { ($_ -split ' ')[2] } | 
    Select-Object Name, Count | 
    Sort-Object Count -Descending |
    Format-Table
```

---

## Overview

### 🎯 Objective
Successfully deploy Medical Lab Analyzer on Windows Server 2025 ensuring:
- High stability and performance
- Security and protection
- Continuous monitoring
- Easy maintenance

### 📊 Deployment Architecture
```
Development → GitHub Actions → Windows Server 2025
     ↓             ↓                 ↓
Build Tests   CI/CD Pipeline   Production App
Local Tests   Automated Tests  Live Monitoring
```

---

## Deployment Requirements

### 🖥️ Server Requirements
| Component | Minimum | Recommended | Notes |
|-----------|---------|-------------|-------|
| **OS** | Windows Server 2025 Standard | Windows Server 2025 Datacenter | 64-bit |
| **CPU** | 4 Cores, 2.4GHz | 8 Cores, 3.0GHz+ | Intel/AMD x64 |
| **Memory** | 8 GB RAM | 16 GB RAM+ | For AI processing |
| **Storage** | 100 GB SSD | 500 GB NVMe SSD | High performance |
| **Network** | 1 Gbps | 10 Gbps | For cloud services |

### 🔧 Required Software
```powershell
# .NET 8.0 Runtime
winget install Microsoft.DotNet.Runtime.8

# IIS (optional for Web API)
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole

# PowerShell 7.x
winget install Microsoft.PowerShell

# Visual C++ Redistributables
winget install Microsoft.VCRedist.2015+.x64
```

---

## Pre-Deployment Setup

### 🔐 Security Setup
```powershell
# Create system user
$securePassword = ConvertTo-SecureString "MedLabAnalyzer2025!" -AsPlainText -Force
New-LocalUser -Name "MedLabService" -Password $securePassword -Description "Medical Lab Analyzer Service Account"

# Set permissions
Add-LocalGroupMember -Group "Users" -Member "MedLabService"
Add-LocalGroupMember -Group "IIS_IUSRS" -Member "MedLabService"
```

### 📁 Directory Setup
```powershell
# Create directory structure
$appPath = "C:\MedicalLabAnalyzer"
$logPath = "C:\MedicalLabAnalyzer\Logs"
$dataPath = "C:\MedicalLabAnalyzer\Data"
$backupPath = "C:\MedicalLabAnalyzer\Backups"

New-Item -Path $appPath, $logPath, $dataPath, $backupPath -ItemType Directory -Force

# Set permissions
icacls $appPath /grant "MedLabService:(OI)(CI)F" /T
```

---

## Deployment Process

### 🚀 Automated Deployment (GitHub Actions)
```yaml
# Using pre-configured workflow
name: Deploy to Windows Server 2025
on:
  push:
    branches: [main]
    
jobs:
  deploy:
    runs-on: windows-2025
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Deploy to Server
        run: |
          .\.github\scripts\deploy-to-server.ps1
```

### 📦 Manual Deployment
```powershell
# 1. Clone project
git clone https://github.com/you112ef/Skkky.git
cd Skkky\MedicalLabAnalyzer

# 2. Build for deployment
dotnet publish -c Release -r win-x64 --self-contained true -o "C:\MedicalLabAnalyzer\App"

# 3. Copy files
Copy-Item -Path ".\publish\*" -Destination "C:\MedicalLabAnalyzer\App\" -Recurse -Force

# 4. Install as Windows Service
New-Service -Name "MedicalLabAnalyzer" -BinaryPathName "C:\MedicalLabAnalyzer\App\MedicalLabAnalyzer.exe" -StartupType Automatic

# 5. Start service
Start-Service -Name "MedicalLabAnalyzer"
```

---

## Monitoring & Maintenance

### 📊 Performance Counter Setup
```powershell
# Create custom performance counters
$categoryName = "Medical Lab Analyzer"
$counters = @(
    @{Name="Active Users"; Type="NumberOfItems32"},
    @{Name="Database Queries/sec"; Type="RateOfCountsPerSecond32"},
    @{Name="AI Processing Time"; Type="AverageTimer32"},
    @{Name="Memory Usage (MB)"; Type="NumberOfItems32"}
)

# Register counters
foreach ($counter in $counters) {
    New-PerformanceCounterCategory -CategoryName $categoryName -CounterName $counter.Name -CounterType $counter.Type
}
```

### 🔍 Monitoring System
```powershell
# Specialized monitoring script
# File: Monitor-MedicalLabAnalyzer.ps1

param(
    [int]$CheckInterval = 300,  # 5 minutes
    [string]$AlertEmail = "admin@medicallab.com"
)

while ($true) {
    # Check service health
    $service = Get-Service -Name "MedicalLabAnalyzer" -ErrorAction SilentlyContinue
    
    if ($service.Status -ne "Running") {
        Send-MailMessage -To $AlertEmail -Subject "Alert: Lab Service Down" -Body "Service stopped at $(Get-Date)"
        Start-Service -Name "MedicalLabAnalyzer"
    }
    
    # Check memory usage
    $process = Get-Process -Name "MedicalLabAnalyzer" -ErrorAction SilentlyContinue
    if ($process -and $process.WorkingSet64 -gt 2GB) {
        Write-Warning "High memory usage: $([math]::Round($process.WorkingSet64/1GB, 2)) GB"
    }
    
    # Check disk space
    $disk = Get-WmiObject -Class Win32_LogicalDisk -Filter "DriveType=3"
    $freeSpace = ($disk.FreeSpace / $disk.Size) * 100
    
    if ($freeSpace -lt 20) {
        Send-MailMessage -To $AlertEmail -Subject "Alert: Low Disk Space" -Body "Free space: $([math]::Round($freeSpace, 2))%"
    }
    
    Start-Sleep -Seconds $CheckInterval
}
```

---

## Troubleshooting

### 🚨 Common Issues and Solutions

#### 1. Service Won't Start
```powershell
# Diagnosis
Get-EventLog -LogName Application -Source "Medical Lab Analyzer" -Newest 10

# Potential solutions
# A. Check permissions
icacls "C:\MedicalLabAnalyzer" /verify

# B. Check dependencies
Get-Service -Name "MedicalLabAnalyzer" | Select-Object -ExpandProperty RequiredServices

# C. Reinstall service
sc delete "MedicalLabAnalyzer"
New-Service -Name "MedicalLabAnalyzer" -BinaryPathName "C:\MedicalLabAnalyzer\App\MedicalLabAnalyzer.exe"
```

#### 2. Database Connection Issues
```powershell
# Check database file
Test-Path "C:\MedicalLabAnalyzer\Data\medicallab.db"

# Check permissions
icacls "C:\MedicalLabAnalyzer\Data\medicallab.db"

# Restore from backup
$latestBackup = Get-ChildItem "C:\MedicalLabAnalyzer\Backups\" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Copy-Item $latestBackup.FullName "C:\MedicalLabAnalyzer\Data\medicallab.db" -Force
```

---

## 📞 Support & Resources

### الدعم بالعربية
- 🔧 **مشاكل تقنية**: [GitHub Issues](https://github.com/you112ef/Skkky/issues)
- 📧 **البريد الإلكتروني**: support@medicallab.com
- 📱 **الدعم العاجل**: +966-11-1234567

### English Support
- 🔧 **Technical Issues**: [GitHub Issues](https://github.com/you112ef/Skkky/issues)
- 📧 **Email Support**: support@medicallab.com
- 📱 **Emergency Support**: +966-11-1234567

---

## 📄 Resources & Documentation

### 📚 Additional Resources
- [Windows Server 2025 Documentation](https://docs.microsoft.com/en-us/windows-server/)
- [.NET 8.0 Deployment Guide](https://docs.microsoft.com/en-us/dotnet/core/deploying/)
- [PowerShell DSC](https://docs.microsoft.com/en-us/powershell/scripting/dsc/)
- [Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/)

### 🔧 Configuration Templates
- [Production appsettings.json](templates/appsettings.production.json)
- [Monitoring Scripts](scripts/monitoring/)
- [Backup Scripts](scripts/backup/)
- [Performance Tuning](docs/performance-tuning.md)

---

**© 2025 Medical Lab Analyzer - Windows Server 2025 Deployment Guide**

*Last Updated: January 17, 2025*