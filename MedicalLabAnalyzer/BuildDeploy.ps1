# MedicalLabAnalyzer Build and Deploy Script
# Author: Scout AI
# Description: Complete build, package, and deployment script for offline medical lab system
# Usage: .\BuildDeploy.ps1 [Options]

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64", 
    [switch]$SkipBuild = $false,
    [switch]$CreateInstaller = $false,
    [switch]$IncludeDebugSymbols = $false,
    [switch]$CompressOutput = $true,
    [string]$OutputPath = ".\Deploy"
)

# Script configuration
$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date
$ProjectName = "MedicalLabAnalyzer"
$ProjectFile = "$ProjectName.csproj"
$SolutionFile = "$ProjectName.sln"

# Colors for console output
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    } else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Error { Write-ColorOutput Red $args }
function Write-Info { Write-ColorOutput Cyan $args }

# Header
Clear-Host
Write-Info "=================================================================="
Write-Info "    Medical Lab Analyzer - Build & Deploy Script v1.0"
Write-Info "    نظام إدارة المختبرات الطبية مع الذكاء الاصطناعي"
Write-Info "=================================================================="
Write-Info ""

# Check prerequisites
Write-Info "📋 Checking prerequisites..."

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Success "✅ .NET SDK found: $dotnetVersion"
} catch {
    Write-Error "❌ .NET SDK not found. Please install .NET 8.0 SDK"
    exit 1
}

# Check project files
if (!(Test-Path $ProjectFile)) {
    Write-Error "❌ Project file not found: $ProjectFile"
    exit 1
}
Write-Success "✅ Project file found: $ProjectFile"

if (!(Test-Path $SolutionFile)) {
    Write-Warning "⚠️ Solution file not found: $SolutionFile (optional)"
} else {
    Write-Success "✅ Solution file found: $SolutionFile"
}

# Create output directories
Write-Info ""
Write-Info "📁 Creating output directories..."
$BuildPath = "$OutputPath\Build"
$PackagePath = "$OutputPath\Package"
$TempPath = "$OutputPath\Temp"

foreach ($path in @($OutputPath, $BuildPath, $PackagePath, $TempPath)) {
    if (!(Test-Path $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
        Write-Success "✅ Created: $path"
    }
}

# Build project
if (!$SkipBuild) {
    Write-Info ""
    Write-Info "🔨 Building project..."
    Write-Info "Configuration: $Configuration"
    Write-Info "Platform: $Platform"
    
    # Restore packages
    Write-Info "📦 Restoring NuGet packages..."
    dotnet restore $ProjectFile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Package restore failed"
        exit 1
    }
    Write-Success "✅ Packages restored successfully"
    
    # Build
    Write-Info "🔧 Building application..."
    $buildArgs = @(
        "build",
        $ProjectFile,
        "--configuration", $Configuration,
        "--output", $BuildPath,
        "--verbosity", "minimal"
    )
    
    if (!$IncludeDebugSymbols) {
        $buildArgs += "--no-debug"
    }
    
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Build failed"
        exit 1
    }
    Write-Success "✅ Build completed successfully"
}

# Publish self-contained
Write-Info ""
Write-Info "📦 Publishing self-contained application..."
$publishPath = "$TempPath\Publish"
$publishArgs = @(
    "publish",
    $ProjectFile,
    "--configuration", $Configuration,
    "--runtime", "win-x64",
    "--self-contained", "true",
    "--output", $publishPath,
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:PublishTrimmed=false"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Publish failed"
    exit 1
}
Write-Success "✅ Application published successfully"

# Copy additional files
Write-Info ""
Write-Info "📁 Copying additional files..."

# Create package structure
$packageStructure = @{
    "Database" = "Database"
    "AI\Config" = "AI\Config"
    "AI\YOLOv8" = "AI\YOLOv8"
    "AI\DeepSORT" = "AI\DeepSORT"
    "Reports\Templates" = "Reports\Templates"
    "Resources" = "Resources"
}

foreach ($folder in $packageStructure.Keys) {
    $sourcePath = $folder
    $destPath = "$PackagePath\$($packageStructure[$folder])"
    
    if (Test-Path $sourcePath) {
        Write-Info "📂 Copying $sourcePath..."
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        Write-Success "✅ Copied: $sourcePath -> $destPath"
    } else {
        Write-Warning "⚠️ Source not found: $sourcePath (creating empty directory)"
        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
    }
}

# Copy published application
Write-Info "📂 Copying application files..."
Copy-Item -Path "$publishPath\*" -Destination $PackagePath -Recurse -Force
Write-Success "✅ Application files copied"

# Copy important files
$importantFiles = @(
    "README.md",
    "LICENSE",
    "CHANGELOG.md"
)

foreach ($file in $importantFiles) {
    if (Test-Path $file) {
        Copy-Item -Path $file -Destination $PackagePath -Force
        Write-Success "✅ Copied: $file"
    }
}

# Create configuration files
Write-Info ""
Write-Info "⚙️ Creating configuration files..."

# App configuration
$appConfig = @"
{
  "DatabasePath": "Database\\medical_lab.db",
  "BackupPath": "Database\\Backup",
  "ReportsPath": "Reports\\GeneratedReports",
  "TempPath": "Temp",
  "AI": {
    "YOLOv8ModelPath": "AI\\YOLOv8\\yolov8n_sperm.onnx",
    "DeepSORTModelPath": "AI\\DeepSORT\\deep_sort_features.onnx",
    "ConfigPath": "AI\\Config"
  },
  "Logging": {
    "LogLevel": "Information",
    "LogPath": "Logs"
  },
  "Security": {
    "SessionTimeoutMinutes": 30,
    "MaxLoginAttempts": 5,
    "PasswordMinLength": 8
  }
}
"@

$appConfig | Out-File -FilePath "$PackagePath\appsettings.json" -Encoding UTF8
Write-Success "✅ Created: appsettings.json"

# Create batch file for easy startup
$batchContent = @"
@echo off
echo ================================================================
echo    نظام إدارة المختبرات الطبية مع الذكاء الاصطناعي
echo    Medical Lab Analyzer with AI
echo ================================================================
echo.
echo Starting Medical Lab Analyzer...
echo.

REM Create necessary directories
if not exist "Database\Backup" mkdir "Database\Backup"
if not exist "Reports\GeneratedReports" mkdir "Reports\GeneratedReports"
if not exist "Logs" mkdir "Logs"
if not exist "Temp" mkdir "Temp"

REM Start the application
start "" "$ProjectName.exe"

echo Application started successfully!
echo.
pause
"@

$batchContent | Out-File -FilePath "$PackagePath\StartMedicalLabAnalyzer.bat" -Encoding ASCII
Write-Success "✅ Created: StartMedicalLabAnalyzer.bat"

# Create installation instructions
$installInstructions = @"
# Medical Lab Analyzer - Installation Instructions
# تعليمات تثبيت نظام إدارة المختبرات الطبية

## متطلبات النظام / System Requirements

### الحد الأدنى / Minimum Requirements:
- Windows 10 (64-bit) or later
- 4 GB RAM
- 2 GB free disk space
- DirectX 11 compatible graphics card

### الموصى به / Recommended:
- Windows 11 (64-bit)
- 8 GB RAM or more
- 4 GB free disk space
- Dedicated graphics card with CUDA support (for AI acceleration)
- USB ports for camera/microscope connection

## خطوات التثبيت / Installation Steps

### 1. استخراج الملفات / Extract Files
- Extract all files to a directory (e.g., C:\MedicalLabAnalyzer)
- احتفظ بهيكل المجلدات كما هو

### 2. تشغيل التطبيق / Run Application
- Double-click "StartMedicalLabAnalyzer.bat" OR
- Run "MedicalLabAnalyzer.exe" directly

### 3. الإعداد الأولي / Initial Setup
- First login: Username: admin, Password: admin
- Change default passwords immediately
- Configure AI models (see AI Setup section)

## إعداد نماذج الذكاء الاصطناعي / AI Models Setup

### YOLOv8 Model:
1. Download or train a YOLOv8 model for sperm detection
2. Place the .onnx file in: AI\YOLOv8\yolov8n_sperm.onnx
3. See AI\YOLOv8\README.md for detailed instructions

### DeepSORT Model:
1. Download DeepSORT feature extraction model
2. Place the .onnx file in: AI\DeepSORT\deep_sort_features.onnx
3. See AI\DeepSORT\README.md for detailed instructions

## استخدام النظام / Using the System

### المستخدمون الافتراضيون / Default Users:
- **admin/admin** - Administrator (full access)
- **lab/123** - Lab Technician (analysis and reports)
- **reception/123** - Receptionist (patient management)

### الوظائف الرئيسية / Main Features:
- إدارة المرضى / Patient Management
- تحليل CASA بالذكاء الاصطناعي / AI-powered CASA Analysis
- تحليل الصور الطبية / Medical Image Analysis
- 17 نوع من التحاليل الطبية / 17 Medical Test Types
- تقارير PDF/Excel / PDF/Excel Reports
- نظام الصلاحيات / Permission System

## استكشاف الأخطاء / Troubleshooting

### التطبيق لا يبدأ / Application Won't Start:
- Check Windows Event Viewer for error details
- Ensure .NET 8.0 Runtime is installed
- Run as Administrator if needed

### نماذج الذكاء الاصطناعي لا تعمل / AI Models Not Working:
- Verify model files are in correct locations
- Check model file formats (.onnx required)
- See AI configuration files in AI\Config\

### قاعدة البيانات / Database Issues:
- Database file: Database\medical_lab.db
- Backups stored in: Database\Backup\
- Delete database file to reset (will lose all data)

## الدعم الفني / Technical Support

### ملفات السجل / Log Files:
- Application logs: Logs\
- Error details available in Windows Event Viewer

### النسخ الاحتياطي / Backup:
- Database backups: Database\Backup\
- Export data regularly through the application

### التحديثات / Updates:
- Check GitHub repository for updates
- Follow semantic versioning (v1.0.0, v1.1.0, etc.)

## الأمان / Security

### كلمات المرور / Passwords:
- Change all default passwords on first use
- Use strong passwords (8+ characters)
- Enable account lockout protection

### الصلاحيات / Permissions:
- Three user levels: Manager, Lab Technician, Receptionist
- Configure permissions based on job requirements
- Regular audit of user access

## المطابقة للمعايير / Standards Compliance

### CASA Analysis:
- WHO 2010 standards compliance
- Automated quality control
- Calibration procedures included

### Medical Device:
- Designed for IVF/Andrology laboratories
- Follow local regulations for medical devices
- Validate results before clinical use

---

للحصول على الدعم، راجع الوثائق أو اتصل بفريق الدعم الفني
For support, refer to documentation or contact technical support team
"@

$installInstructions | Out-File -FilePath "$PackagePath\INSTALLATION.md" -Encoding UTF8
Write-Success "✅ Created: INSTALLATION.md"

# Create version info
$versionInfo = @"
Medical Lab Analyzer v1.0.0
Build Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Configuration: $Configuration
Platform: $Platform

Components:
- .NET 8.0 WPF Application
- Entity Framework Core with SQLite
- YOLOv8 + DeepSORT AI Integration
- OpenCV Image Processing
- MaterialDesign UI (Arabic RTL)
- 17 Medical Test Analyzers
- WHO 2010 CASA Standards Compliance

Features:
✅ Complete Offline Operation
✅ AI-Powered CASA Analysis
✅ Medical Image Analysis with Measurement Tools
✅ 17 Different Medical Test Types
✅ Arabic RTL Interface
✅ Multi-Level User Permissions
✅ PDF/Excel Reporting
✅ Comprehensive Audit Logging
✅ Real-time AI Status Monitoring
✅ Patient and Exam Management
✅ Image/Video Analysis with Drag-and-Drop
✅ Calibration and Quality Control

Supported File Formats:
- Images: .jpg, .png, .bmp, .tiff
- Videos: .mp4, .avi, .mov, .mkv
- Reports: .pdf, .xlsx, .docx
- Data: .csv, .json, .xml

AI Models Required:
- YOLOv8 ONNX model for sperm detection
- DeepSORT ONNX model for object tracking
- See AI\ directories for setup instructions

Database:
- SQLite with Entity Framework Core
- Automatic migrations and backup
- Local storage (no cloud dependency)

System Requirements:
- Windows 10/11 (64-bit)
- 4+ GB RAM (8+ GB recommended)
- 2+ GB disk space
- GPU with CUDA support (optional, for AI acceleration)
"@

$versionInfo | Out-File -FilePath "$PackagePath\VERSION.txt" -Encoding UTF8
Write-Success "✅ Created: VERSION.txt"

# Compress package
if ($CompressOutput) {
    Write-Info ""
    Write-Info "🗜️ Creating compressed package..."
    
    $zipPath = "$OutputPath\$ProjectName-v1.0.0-$(Get-Date -Format 'yyyyMMdd').zip"
    
    # Use native PowerShell compression
    Compress-Archive -Path "$PackagePath\*" -DestinationPath $zipPath -Force
    
    if (Test-Path $zipPath) {
        $zipSize = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
        Write-Success "✅ Package created: $zipPath ($zipSize MB)"
    } else {
        Write-Error "❌ Failed to create package"
    }
}

# Create installer (optional)
if ($CreateInstaller) {
    Write-Info ""
    Write-Info "📦 Creating MSI installer..."
    
    # Check for WiX Toolset
    $wixPath = Get-Command "heat.exe" -ErrorAction SilentlyContinue
    if ($wixPath) {
        Write-Info "🔧 WiX Toolset found, creating installer..."
        
        # Generate WiX source files
        $wxsFile = "$TempPath\Product.wxs"
        $msiFile = "$OutputPath\$ProjectName-Setup-v1.0.0.msi"
        
        # Heat to generate component list
        & heat.exe dir $PackagePath -cg ApplicationFiles -gg -scom -sreg -sfrag -srd -dr INSTALLDIR -out "$TempPath\Directory.wxs"
        
        # Create main WiX file
        $wixContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="Medical Lab Analyzer" Language="1033" Version="1.0.0" 
           Manufacturer="Medical Lab Solutions" UpgradeCode="12345678-1234-1234-1234-123456789012">
    
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    
    <MediaTemplate EmbedCab="yes" />
    
    <Feature Id="ProductFeature" Title="Medical Lab Analyzer" Level="1">
      <ComponentGroupRef Id="ApplicationFiles" />
    </Feature>
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLDIR" Name="Medical Lab Analyzer" />
      </Directory>
    </Directory>
    
  </Product>
</Wix>
"@
        
        $wixContent | Out-File -FilePath $wxsFile -Encoding UTF8
        
        # Compile
        & candle.exe $wxsFile "$TempPath\Directory.wxs" -out $TempPath\
        & light.exe "$TempPath\Product.wixobj" "$TempPath\Directory.wixobj" -out $msiFile
        
        if (Test-Path $msiFile) {
            $msiSize = [math]::Round((Get-Item $msiFile).Length / 1MB, 2)
            Write-Success "✅ MSI installer created: $msiFile ($msiSize MB)"
        }
    } else {
        Write-Warning "⚠️ WiX Toolset not found. MSI installer skipped."
        Write-Info "   Download from: https://wixtoolset.org/"
    }
}

# Cleanup temp files
Write-Info ""
Write-Info "🧹 Cleaning up temporary files..."
if (Test-Path $TempPath) {
    Remove-Item -Path $TempPath -Recurse -Force
    Write-Success "✅ Temporary files cleaned"
}

# Summary
Write-Info ""
Write-Success "=================================================================="
Write-Success "                     BUILD COMPLETED SUCCESSFULLY!"
Write-Success "=================================================================="
Write-Info ""
Write-Success "📦 Package Location: $PackagePath"
if ($CompressOutput -and (Test-Path $zipPath)) {
    Write-Success "🗜️ ZIP Package: $zipPath"
}
if ($CreateInstaller -and (Test-Path $msiFile)) {
    Write-Success "📦 MSI Installer: $msiFile"
}
Write-Info ""
Write-Info "🚀 Ready for deployment!"
Write-Info "   1. Extract/Install on target machine"
Write-Info "   2. Set up AI models (see AI\ directories)"
Write-Info "   3. Run StartMedicalLabAnalyzer.bat"
Write-Info "   4. Login with default credentials (admin/admin)"
Write-Info "   5. Configure system and change passwords"
Write-Info ""
Write-Success "Build completed in $((Get-Date) - $script:StartTime)"
Write-Info ""