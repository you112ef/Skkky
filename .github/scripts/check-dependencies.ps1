# Medical Lab Analyzer - Dependencies Check Script
# Script to verify all required dependencies for Windows Server 2025 compatibility
# Solves GitHub Actions Issue #12677

param(
    [string]$ProjectType = "desktop",  # desktop, azure-functions, powershell-functions
    [switch]$Detailed = $false,
    [switch]$InstallMissing = $false,
    [switch]$ExportReport = $false,
    [string]$ReportPath = "dependency-report.json"
)

# Initialize results
$script:Results = @{
    "SystemInfo" = @{}
    "Dependencies" = @{}
    "Issues" = @()
    "Warnings" = @()
    "Recommendations" = @()
    "Summary" = @{}
}

# Colors for output
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
Write-Info "================================================================="
Write-Info "  Medical Lab Analyzer - Windows Server 2025 Dependency Check"
Write-Info "  فحص متطلبات نظام إدارة المختبرات الطبية"
Write-Info "================================================================="
Write-Info ""

# System Information Collection
function Get-SystemInformation {
    Write-Info "📋 جمع معلومات النظام / Collecting system information..."
    
    try {
        $os = Get-CimInstance Win32_OperatingSystem
        $computer = Get-CimInstance Win32_ComputerSystem
        $processor = Get-CimInstance Win32_Processor | Select-Object -First 1
        
        $systemInfo = @{
            "OSName" = $os.Caption
            "OSVersion" = $os.Version
            "OSBuild" = $os.BuildNumber
            "Architecture" = $os.OSArchitecture
            "TotalMemoryGB" = [math]::Round($computer.TotalPhysicalMemory / 1GB, 2)
            "ProcessorName" = $processor.Name
            "ProcessorCores" = $processor.NumberOfCores
            "LogicalProcessors" = $processor.NumberOfLogicalProcessors
            "IsWindowsServer2025" = $os.Caption -like "*Server 2025*"
            "IsWindowsServer2022" = $os.Caption -like "*Server 2022*"
            "IsWindowsServer2019" = $os.Caption -like "*Server 2019*"
        }
        
        # Check available drives
        $drives = Get-CimInstance Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 }
        $driveInfo = @{}
        foreach ($drive in $drives) {
            $driveInfo[$drive.DeviceID] = @{
                "SizeGB" = [math]::Round($drive.Size / 1GB, 2)
                "FreeGB" = [math]::Round($drive.FreeSpace / 1GB, 2)
            }
        }
        $systemInfo["Drives"] = $driveInfo
        
        $script:Results.SystemInfo = $systemInfo
        
        Write-Success "✅ نظام التشغيل: $($systemInfo.OSName)"
        Write-Success "✅ المعمارية: $($systemInfo.Architecture)"
        Write-Success "✅ الذاكرة: $($systemInfo.TotalMemoryGB) GB"
        Write-Success "✅ المعالج: $($systemInfo.ProcessorCores) cores, $($systemInfo.LogicalProcessors) logical"
        
        # Windows Server 2025 specific checks
        if ($systemInfo.IsWindowsServer2025) {
            Write-Warning "⚠️ Windows Server 2025 detected - running compatibility checks"
            
            if (-not $systemInfo.Drives.ContainsKey("D:")) {
                $script:Results.Issues += @{
                    "Type" = "Critical"
                    "Component" = "Storage"
                    "Message" = "D: drive not available - this is expected on Windows Server 2025"
                    "Solution" = "Use C: drive for temporary files and workspace"
                }
            }
        }
        
    } catch {
        Write-Error "❌ Failed to collect system information: $($_.Exception.Message)"
        $script:Results.Issues += @{
            "Type" = "Critical"
            "Component" = "System"
            "Message" = "Unable to collect system information"
            "Error" = $_.Exception.Message
        }
    }
}

# .NET SDK Check
function Test-DotNetSDK {
    Write-Info "🔧 فحص .NET SDK / Checking .NET SDK..."
    
    try {
        # Check if dotnet is available
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✅ .NET SDK found: $dotnetVersion"
            
            # Check if it's .NET 8.0 or later
            $version = [Version]::new($dotnetVersion.Split('-')[0])
            if ($version.Major -ge 8) {
                Write-Success "✅ .NET 8.0+ compatible: $dotnetVersion"
                $script:Results.Dependencies["dotnet"] = @{
                    "Status" = "OK"
                    "Version" = $dotnetVersion
                    "Compatible" = $true
                }
            } else {
                Write-Warning "⚠️ .NET version $dotnetVersion may not be fully compatible"
                $script:Results.Dependencies["dotnet"] = @{
                    "Status" = "Warning"
                    "Version" = $dotnetVersion
                    "Compatible" = $false
                    "RequiredVersion" = "8.0.x"
                }
            }
            
            # Check available runtimes
            $runtimes = & dotnet --list-runtimes 2>$null | Out-String
            Write-Info "Available runtimes:"
            Write-Host $runtimes
            
        } else {
            Write-Error "❌ .NET SDK not found"
            $script:Results.Dependencies["dotnet"] = @{
                "Status" = "Missing"
                "Compatible" = $false
                "RequiredVersion" = "8.0.x"
            }
            
            if ($InstallMissing) {
                Write-Info "📦 Installing .NET SDK..."
                Install-DotNetSDK
            }
        }
    } catch {
        Write-Error "❌ Error checking .NET SDK: $($_.Exception.Message)"
        $script:Results.Dependencies["dotnet"] = @{
            "Status" = "Error"
            "Error" = $_.Exception.Message
        }
    }
}

# PowerShell Check
function Test-PowerShell {
    Write-Info "⚡ فحص PowerShell / Checking PowerShell..."
    
    try {
        $psVersion = $PSVersionTable.PSVersion
        Write-Success "✅ PowerShell version: $psVersion"
        Write-Success "✅ PowerShell edition: $($PSVersionTable.PSEdition)"
        
        $compatible = $psVersion.Major -ge 5
        $script:Results.Dependencies["powershell"] = @{
            "Status" = if ($compatible) { "OK" } else { "Incompatible" }
            "Version" = $psVersion.ToString()
            "Edition" = $PSVersionTable.PSEdition
            "Compatible" = $compatible
        }
        
        if ($psVersion.Major -ge 7) {
            Write-Success "✅ PowerShell 7+ detected - excellent for Azure Functions"
        } elseif ($psVersion.Major -eq 5) {
            Write-Warning "⚠️ PowerShell 5.x - consider upgrading to 7.x for better performance"
        }
        
        # Check execution policy
        $executionPolicy = Get-ExecutionPolicy
        Write-Info "Execution Policy: $executionPolicy"
        
        if ($executionPolicy -eq "Restricted") {
            Write-Warning "⚠️ Execution policy is Restricted - this may cause issues"
            $script:Results.Warnings += "PowerShell execution policy is Restricted"
        }
        
    } catch {
        Write-Error "❌ Error checking PowerShell: $($_.Exception.Message)"
        $script:Results.Dependencies["powershell"] = @{
            "Status" = "Error"
            "Error" = $_.Exception.Message
        }
    }
}

# MSBuild Check
function Test-MSBuild {
    Write-Info "🔨 فحص MSBuild / Checking MSBuild..."
    
    try {
        # Try different MSBuild locations
        $msbuildPaths = @(
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
            "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
            "${env:ProgramFiles}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
            "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        )
        
        $msbuildFound = $false
        foreach ($path in $msbuildPaths) {
            if (Test-Path $path) {
                try {
                    $version = & $path -version | Select-String "Microsoft" | Select-Object -First 1
                    Write-Success "✅ MSBuild found: $version"
                    $msbuildFound = $true
                    $script:Results.Dependencies["msbuild"] = @{
                        "Status" = "OK"
                        "Path" = $path
                        "Compatible" = $true
                    }
                    break
                } catch {
                    continue
                }
            }
        }
        
        # Try dotnet build as fallback
        if (-not $msbuildFound) {
            try {
                & dotnet build --help >$null 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "✅ MSBuild available through dotnet CLI"
                    $script:Results.Dependencies["msbuild"] = @{
                        "Status" = "OK"
                        "Method" = "dotnet-cli"
                        "Compatible" = $true
                    }
                    $msbuildFound = $true
                }
            } catch {
                # Continue to error handling
            }
        }
        
        if (-not $msbuildFound) {
            Write-Error "❌ MSBuild not found"
            $script:Results.Dependencies["msbuild"] = @{
                "Status" = "Missing"
                "Compatible" = $false
            }
        }
        
    } catch {
        Write-Error "❌ Error checking MSBuild: $($_.Exception.Message)"
        $script:Results.Dependencies["msbuild"] = @{
            "Status" = "Error"
            "Error" = $_.Exception.Message
        }
    }
}

# Git Check
function Test-Git {
    Write-Info "📝 فحص Git / Checking Git..."
    
    try {
        $gitVersion = & git --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✅ Git found: $gitVersion"
            $script:Results.Dependencies["git"] = @{
                "Status" = "OK"
                "Version" = $gitVersion
                "Compatible" = $true
            }
        } else {
            Write-Error "❌ Git not found"
            $script:Results.Dependencies["git"] = @{
                "Status" = "Missing"
                "Compatible" = $false
            }
        }
    } catch {
        Write-Error "❌ Error checking Git: $($_.Exception.Message)"
        $script:Results.Dependencies["git"] = @{
            "Status" = "Error"
            "Error" = $_.Exception.Message
        }
    }
}

# Node.js Check (for some tools)
function Test-NodeJS {
    Write-Info "🟢 فحص Node.js / Checking Node.js..."
    
    try {
        $nodeVersion = & node --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✅ Node.js found: $nodeVersion"
            $script:Results.Dependencies["nodejs"] = @{
                "Status" = "OK"
                "Version" = $nodeVersion
                "Compatible" = $true
            }
            
            # Check npm
            $npmVersion = & npm --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "✅ npm found: $npmVersion"
            }
        } else {
            Write-Info "ℹ️ Node.js not found (optional for this project)"
            $script:Results.Dependencies["nodejs"] = @{
                "Status" = "Not Required"
                "Compatible" = $true
            }
        }
    } catch {
        Write-Info "ℹ️ Node.js check failed (optional): $($_.Exception.Message)"
        $script:Results.Dependencies["nodejs"] = @{
            "Status" = "Not Required"
            "Compatible" = $true
        }
    }
}

# Medical Lab Analyzer Specific Dependencies
function Test-MedicalLabDependencies {
    Write-Info "🏥 فحص متطلبات المختبر الطبي / Checking Medical Lab specific dependencies..."
    
    # Check for Visual C++ Redistributables (required for OpenCV)
    Write-Info "Checking Visual C++ Redistributables..."
    try {
        $vcRedist = Get-CimInstance Win32_Product | Where-Object { $_.Name -like "*Visual C++*Redistributable*" }
        if ($vcRedist) {
            Write-Success "✅ Visual C++ Redistributables found"
            $script:Results.Dependencies["vcredist"] = @{
                "Status" = "OK"
                "Compatible" = $true
                "Versions" = $vcRedist.Name
            }
        } else {
            Write-Warning "⚠️ Visual C++ Redistributables not found - may be needed for OpenCV"
            $script:Results.Dependencies["vcredist"] = @{
                "Status" = "Warning"
                "Compatible" = $true
                "Note" = "May be required for OpenCV/EmguCV"
            }
        }
    } catch {
        Write-Warning "⚠️ Could not check Visual C++ Redistributables"
    }
    
    # Check GPU and CUDA support (for AI acceleration)
    Write-Info "Checking GPU capabilities..."
    try {
        $gpu = Get-CimInstance Win32_VideoController | Where-Object { $_.Name -notlike "*Basic*" -and $_.Name -notlike "*Generic*" }
        if ($gpu) {
            Write-Success "✅ Dedicated GPU found: $($gpu[0].Name)"
            $script:Results.Dependencies["gpu"] = @{
                "Status" = "Available"
                "Name" = $gpu[0].Name
                "Compatible" = $true
            }
            
            # Check for NVIDIA GPU (CUDA support)
            if ($gpu[0].Name -like "*NVIDIA*") {
                Write-Success "✅ NVIDIA GPU detected - CUDA acceleration possible"
                $script:Results.Recommendations += "Consider installing CUDA toolkit for AI acceleration"
            }
        } else {
            Write-Info "ℹ️ No dedicated GPU found - AI will use CPU"
            $script:Results.Dependencies["gpu"] = @{
                "Status" = "Not Available"
                "Compatible" = $true
                "Note" = "AI processing will use CPU"
            }
        }
    } catch {
        Write-Warning "⚠️ Could not check GPU information"
    }
    
    # Check available disk space for AI models
    $cDrive = Get-CimInstance Win32_LogicalDisk | Where-Object { $_.DeviceID -eq "C:" }
    if ($cDrive) {
        $freeSpaceGB = [math]::Round($cDrive.FreeSpace / 1GB, 2)
        if ($freeSpaceGB -gt 5) {
            Write-Success "✅ Sufficient disk space: $freeSpaceGB GB free"
        } else {
            Write-Warning "⚠️ Low disk space: $freeSpaceGB GB free - AI models may need more space"
            $script:Results.Warnings += "Low disk space detected: $freeSpaceGB GB free"
        }
    }
}

# Project-specific checks
function Test-ProjectDependencies {
    param([string]$Type)
    
    switch ($Type) {
        "desktop" {
            Write-Info "🖥️ فحص متطلبات تطبيق سطح المكتب / Checking desktop application dependencies..."
            
            # Check if project files exist
            if (Test-Path "MedicalLabAnalyzer/MedicalLabAnalyzer.csproj") {
                Write-Success "✅ Project file found"
                $script:Results.Dependencies["project"] = @{
                    "Status" = "OK"
                    "Type" = "Desktop"
                    "ProjectFile" = "MedicalLabAnalyzer/MedicalLabAnalyzer.csproj"
                }
            } else {
                Write-Warning "⚠️ Project file not found at expected location"
                $script:Results.Dependencies["project"] = @{
                    "Status" = "Warning"
                    "Type" = "Desktop"
                    "Note" = "Project file not found at MedicalLabAnalyzer/MedicalLabAnalyzer.csproj"
                }
            }
        }
        
        "azure-functions" {
            Write-Info "⚡ فحص متطلبات Azure Functions / Checking Azure Functions dependencies..."
            
            # Check Azure CLI
            try {
                $azVersion = & az --version 2>$null | Select-String "azure-cli" | Select-Object -First 1
                if ($azVersion) {
                    Write-Success "✅ Azure CLI found: $azVersion"
                    $script:Results.Dependencies["azure-cli"] = @{
                        "Status" = "OK"
                        "Version" = $azVersion.ToString()
                        "Compatible" = $true
                    }
                } else {
                    Write-Warning "⚠️ Azure CLI not found"
                    $script:Results.Dependencies["azure-cli"] = @{
                        "Status" = "Missing"
                        "Compatible" = $false
                        "Note" = "Required for Azure deployments"
                    }
                }
            } catch {
                Write-Info "ℹ️ Azure CLI not found (may not be needed for GitHub Actions)"
            }
        }
        
        "powershell-functions" {
            Write-Info "⚡ فحص متطلبات PowerShell Functions / Checking PowerShell Functions dependencies..."
            
            # Check PowerShell modules
            $modules = @('Az.Accounts', 'Az.Functions', 'Pester')
            foreach ($module in $modules) {
                try {
                    $moduleInfo = Get-Module -Name $module -ListAvailable -ErrorAction SilentlyContinue | Select-Object -First 1
                    if ($moduleInfo) {
                        Write-Success "✅ PowerShell module found: $module ($($moduleInfo.Version))"
                    } else {
                        Write-Warning "⚠️ PowerShell module missing: $module"
                        $script:Results.Dependencies["ps-module-$module"] = @{
                            "Status" = "Missing"
                            "Compatible" = $false
                        }
                    }
                } catch {
                    Write-Warning "⚠️ Could not check PowerShell module: $module"
                }
            }
        }
    }
}

# Windows Server 2025 specific compatibility checks
function Test-Windows2025Compatibility {
    Write-Info "🪟 فحص التوافق مع Windows Server 2025 / Checking Windows Server 2025 compatibility..."
    
    $isServer2025 = $script:Results.SystemInfo.IsWindowsServer2025
    
    if ($isServer2025) {
        Write-Warning "⚠️ Running on Windows Server 2025 - performing compatibility checks"
        
        # Known issues and checks
        $compatibilityIssues = @()
        
        # Check for D: drive availability
        if (-not (Test-Path "D:\")) {
            $compatibilityIssues += @{
                "Issue" = "D: drive not available"
                "Impact" = "Medium"
                "Solution" = "Use C: drive for workspace and temporary files"
                "WorkaroundCode" = 'if (!(Test-Path "D:\")) { $workDir = "C:\workspace" } else { $workDir = "D:\workspace" }'
            }
        }
        
        # Check for software changes
        $softwareToCheck = @{
            "Docker Desktop" = "docker --version"
            "Python" = "python --version"
            "Java" = "java -version"
        }
        
        foreach ($software in $softwareToCheck.Keys) {
            try {
                $command = $softwareToCheck[$software]
                $version = & { Invoke-Expression $command } 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "✅ $software available: $version"
                } else {
                    Write-Warning "⚠️ $software may not be available"
                    $compatibilityIssues += @{
                        "Issue" = "$software not found"
                        "Impact" = "Low"
                        "Solution" = "Install $software if required by your application"
                    }
                }
            } catch {
                Write-Warning "⚠️ Could not check $software"
            }
        }
        
        $script:Results.Dependencies["windows2025-compatibility"] = @{
            "Status" = if ($compatibilityIssues.Count -eq 0) { "OK" } else { "Issues Found" }
            "Issues" = $compatibilityIssues
            "TotalIssues" = $compatibilityIssues.Count
        }
        
        # Recommendations for Windows Server 2025
        $script:Results.Recommendations += "Use explicit drive paths instead of assuming D: drive availability"
        $script:Results.Recommendations += "Test workflows on windows-2022 for comparison"
        $script:Results.Recommendations += "Monitor build times and resource usage"
        
    } else {
        Write-Success "✅ Running on older Windows version - standard compatibility expected"
        $script:Results.Dependencies["windows2025-compatibility"] = @{
            "Status" = "Not Applicable"
            "Note" = "Running on non-Windows Server 2025 system"
        }
    }
}

# Generate recommendations
function Generate-Recommendations {
    Write-Info "💡 إنتاج التوصيات / Generating recommendations..."
    
    # Performance recommendations
    if ($script:Results.SystemInfo.TotalMemoryGB -lt 8) {
        $script:Results.Recommendations += "Consider upgrading to 8GB+ RAM for better performance"
    }
    
    # Security recommendations
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        $script:Results.Recommendations += "Upgrade to PowerShell 7.x for better security and performance"
    }
    
    # AI/GPU recommendations
    $gpu = $script:Results.Dependencies["gpu"]
    if ($gpu -and $gpu.Status -eq "Available" -and $gpu.Name -like "*NVIDIA*") {
        $script:Results.Recommendations += "Install CUDA toolkit for GPU-accelerated AI processing"
    }
    
    # Storage recommendations
    $cDrive = $script:Results.SystemInfo.Drives["C:"]
    if ($cDrive -and $cDrive.FreeGB -lt 10) {
        $script:Results.Recommendations += "Free up disk space - less than 10GB available"
    }
    
    # Windows Server 2025 specific recommendations
    if ($script:Results.SystemInfo.IsWindowsServer2025) {
        $script:Results.Recommendations += "Use 'windows-2022' runner for stable builds during migration period"
        $script:Results.Recommendations += "Implement fallback mechanisms for D: drive dependencies"
        $script:Results.Recommendations += "Add comprehensive error handling for new environment differences"
    }
}

# Install .NET SDK if missing
function Install-DotNetSDK {
    if (-not $InstallMissing) {
        Write-Info "⚠️ .NET SDK missing but InstallMissing not specified"
        return
    }
    
    Write-Info "📦 Installing .NET 8.0 SDK..."
    try {
        # Download and install .NET 8.0 SDK
        $url = "https://download.visualstudio.microsoft.com/download/pr/5226a5fa-8c0b-474f-b79a-8984ad7c5beb/3113ccbf789c9fd29972835f0f334b7a/dotnet-sdk-8.0.303-win-x64.exe"
        $output = "$env:TEMP\dotnet-sdk-installer.exe"
        
        Write-Info "Downloading .NET SDK installer..."
        Invoke-WebRequest -Uri $url -OutFile $output
        
        Write-Info "Installing .NET SDK..."
        Start-Process -FilePath $output -ArgumentList "/quiet" -Wait
        
        Write-Success "✅ .NET SDK installation completed"
        
        # Verify installation
        $env:PATH = [Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [Environment]::GetEnvironmentVariable("PATH", "User")
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "✅ .NET SDK verified: $dotnetVersion"
        }
        
    } catch {
        Write-Error "❌ Failed to install .NET SDK: $($_.Exception.Message)"
    }
}

# Export report to JSON
function Export-Report {
    param([string]$Path)
    
    Write-Info "📄 تصدير التقرير / Exporting report to $Path..."
    
    try {
        # Add summary information
        $totalDependencies = $script:Results.Dependencies.Count
        $okDependencies = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "OK" }).Count
        $missingDependencies = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "Missing" }).Count
        $errorDependencies = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "Error" }).Count
        
        $script:Results.Summary = @{
            "TotalDependencies" = $totalDependencies
            "OKDependencies" = $okDependencies
            "MissingDependencies" = $missingDependencies
            "ErrorDependencies" = $errorDependencies
            "TotalIssues" = $script:Results.Issues.Count
            "TotalWarnings" = $script:Results.Warnings.Count
            "TotalRecommendations" = $script:Results.Recommendations.Count
            "OverallStatus" = if ($missingDependencies -eq 0 -and $errorDependencies -eq 0) { "PASS" } else { "ISSUES_FOUND" }
            "CheckDate" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
            "ProjectType" = $ProjectType
        }
        
        $json = $script:Results | ConvertTo-Json -Depth 10
        $json | Out-File -FilePath $Path -Encoding UTF8
        
        Write-Success "✅ Report exported to: $Path"
    } catch {
        Write-Error "❌ Failed to export report: $($_.Exception.Message)"
    }
}

# Main execution
Write-Info "🔍 بدء فحص المتطلبات / Starting dependency check for project type: $ProjectType"
Write-Info ""

# Run all checks
Get-SystemInformation
Test-DotNetSDK
Test-PowerShell
Test-MSBuild
Test-Git
Test-NodeJS
Test-MedicalLabDependencies
Test-ProjectDependencies -Type $ProjectType
Test-Windows2025Compatibility
Generate-Recommendations

# Print summary
Write-Info ""
Write-Info "📊 ملخص النتائج / Results Summary"
Write-Info "=================================="

$totalDeps = $script:Results.Dependencies.Count
$okDeps = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "OK" }).Count
$missingDeps = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "Missing" }).Count
$errorDeps = ($script:Results.Dependencies.Values | Where-Object { $_.Status -eq "Error" }).Count

Write-Info "Total Dependencies Checked: $totalDeps"
Write-Success "✅ OK: $okDeps"
if ($missingDeps -gt 0) { Write-Warning "⚠️ Missing: $missingDeps" }
if ($errorDeps -gt 0) { Write-Error "❌ Errors: $errorDeps" }
Write-Info "Issues Found: $($script:Results.Issues.Count)"
Write-Info "Warnings: $($script:Results.Warnings.Count)"
Write-Info "Recommendations: $($script:Results.Recommendations.Count)"

if ($script:Results.Issues.Count -gt 0) {
    Write-Info ""
    Write-Error "🚨 مشاكل حرجة / Critical Issues Found:"
    foreach ($issue in $script:Results.Issues) {
        Write-Error "   • $($issue.Component): $($issue.Message)"
        if ($issue.Solution) {
            Write-Info "     Solution: $($issue.Solution)"
        }
    }
}

if ($script:Results.Warnings.Count -gt 0) {
    Write-Info ""
    Write-Warning "⚠️ تحذيرات / Warnings:"
    foreach ($warning in $script:Results.Warnings) {
        Write-Warning "   • $warning"
    }
}

if ($script:Results.Recommendations.Count -gt 0) {
    Write-Info ""
    Write-Info "💡 توصيات / Recommendations:"
    foreach ($recommendation in $script:Results.Recommendations) {
        Write-Info "   • $recommendation"
    }
}

# Overall status
Write-Info ""
if ($missingDeps -eq 0 -and $errorDeps -eq 0 -and $script:Results.Issues.Count -eq 0) {
    Write-Success "🎉 جميع المتطلبات متوفرة! / All dependencies satisfied!"
    $exitCode = 0
} elseif ($errorDeps -gt 0 -or $script:Results.Issues.Count -gt 0) {
    Write-Error "💥 مشاكل حرجة وجدت / Critical issues found - build may fail"
    $exitCode = 1
} else {
    Write-Warning "⚠️ تحذيرات موجودة لكن البناء قد ينجح / Warnings present but build should proceed"
    $exitCode = 0
}

# Export report if requested
if ($ExportReport) {
    Export-Report -Path $ReportPath
}

Write-Info ""
Write-Info "================================================================="
Write-Info "  فحص المتطلبات مكتمل / Dependency check completed"
Write-Info "  نتيجة الفحص / Check result: $(if ($exitCode -eq 0) { 'PASS' } else { 'FAIL' })"
Write-Info "================================================================="

exit $exitCode