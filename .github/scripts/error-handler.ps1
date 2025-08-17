# Medical Lab Analyzer - Enhanced Error Handling for GitHub Actions
# Windows Server 2025 compatibility error handler
# Solves GitHub Actions Issue #12677

param(
    [string]$ErrorContext = "General",
    [string]$LogFile = "error-log.json",
    [switch]$RetryEnabled = $false,
    [int]$MaxRetries = 3,
    [switch]$FallbackToWindows2022 = $false
)

# Global error tracking
$script:ErrorLog = @{
    "Timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
    "Context" = $ErrorContext
    "Errors" = @()
    "System" = @{}
    "Recovery" = @{}
}

# Initialize system info
$script:ErrorLog.System = @{
    "OS" = (Get-CimInstance Win32_OperatingSystem).Caption
    "RunnerOS" = $env:RUNNER_OS
    "GitHubActor" = $env:GITHUB_ACTOR
    "GitHubRef" = $env:GITHUB_REF
    "GitHubSHA" = $env:GITHUB_SHA
    "WorkflowName" = $env:GITHUB_WORKFLOW
    "JobName" = $env:GITHUB_JOB
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

# Enhanced error handler function
function Handle-Error {
    param(
        [Parameter(Mandatory)]
        [string]$ErrorMessage,
        
        [string]$ErrorType = "Unknown",
        [string]$Component = "General",
        [string]$Solution = "",
        [scriptblock]$RecoveryAction = $null,
        [bool]$IsCritical = $true,
        [string]$WorkaroundCode = ""
    )
    
    $errorEntry = @{
        "Timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss UTC"
        "Type" = $ErrorType
        "Component" = $Component
        "Message" = $ErrorMessage
        "Solution" = $Solution
        "IsCritical" = $IsCritical
        "WorkaroundCode" = $WorkaroundCode
        "RecoveryAttempted" = $false
        "RecoverySuccessful" = $false
    }
    
    # Add to error log
    $script:ErrorLog.Errors += $errorEntry
    
    # Display error
    if ($IsCritical) {
        Write-Error "💥 CRITICAL ERROR [$Component]: $ErrorMessage"
    } else {
        Write-Warning "⚠️ WARNING [$Component]: $ErrorMessage"
    }
    
    if ($Solution) {
        Write-Info "💡 Solution: $Solution"
    }
    
    if ($WorkaroundCode) {
        Write-Info "🔧 Workaround code:"
        Write-Host $WorkaroundCode -ForegroundColor Yellow
    }
    
    # Attempt recovery if provided
    if ($RecoveryAction -and ($RetryEnabled -or $IsCritical)) {
        Write-Info "🔄 Attempting recovery action..."
        $errorEntry.RecoveryAttempted = $true
        
        try {
            & $RecoveryAction
            $errorEntry.RecoverySuccessful = $true
            Write-Success "✅ Recovery successful"
        } catch {
            $errorEntry.RecoverySuccessful = $false
            $errorEntry.RecoveryError = $_.Exception.Message
            Write-Error "❌ Recovery failed: $($_.Exception.Message)"
        }
    }
    
    return $errorEntry
}

# Windows Server 2025 specific error handlers
function Handle-DriveNotFound {
    param([string]$DriveLetter = "D")
    
    $errorDetails = Handle-Error -ErrorMessage "Drive $DriveLetter:\ not found" -ErrorType "Windows2025Compatibility" -Component "Storage" -Solution "Use C:\ drive for workspace and temporary files" -IsCritical $false -WorkaroundCode @"
if (!(Test-Path "$DriveLetter:\")) {
    Write-Warning "Drive $DriveLetter:\ not available, using C:\ instead"
    `$workspaceDir = "C:\workspace"
} else {
    `$workspaceDir = "$DriveLetter:\workspace"
}
New-Item -ItemType Directory -Path `$workspaceDir -Force | Out-Null
"@ -RecoveryAction {
        # Create workspace on C: drive
        $workspaceDir = "C:\workspace"
        New-Item -ItemType Directory -Path $workspaceDir -Force | Out-Null
        Write-Output "WORKSPACE_DIR=$workspaceDir" >> $env:GITHUB_ENV
        Write-Success "Created workspace directory: $workspaceDir"
    }
    
    return $errorDetails
}

function Handle-SoftwareMissing {
    param(
        [string]$SoftwareName,
        [string]$InstallCommand = "",
        [string]$DownloadUrl = ""
    )
    
    $solution = if ($InstallCommand) { "Run: $InstallCommand" } elseif ($DownloadUrl) { "Download from: $DownloadUrl" } else { "Install $SoftwareName manually" }
    
    $errorDetails = Handle-Error -ErrorMessage "$SoftwareName not found or not working" -ErrorType "MissingSoftware" -Component "Dependencies" -Solution $solution -IsCritical $true -RecoveryAction {
        if ($InstallCommand) {
            Write-Info "Attempting to install $SoftwareName..."
            Invoke-Expression $InstallCommand
        } else {
            Write-Warning "No automatic installation available for $SoftwareName"
        }
    }
    
    return $errorDetails
}

function Handle-DotNetError {
    param([string]$SpecificError = "")
    
    $errorDetails = Handle-Error -ErrorMessage ".NET SDK error: $SpecificError" -ErrorType "DotNetError" -Component "BuildTools" -Solution "Verify .NET 8.0 SDK installation" -IsCritical $true -WorkaroundCode @"
# Check .NET installation
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes

# Try to restore packages explicitly
dotnet restore --verbosity detailed
"@ -RecoveryAction {
        Write-Info "Checking .NET installation..."
        
        try {
            $dotnetInfo = & dotnet --info 2>&1
            Write-Host $dotnetInfo
            
            $sdks = & dotnet --list-sdks 2>&1
            Write-Info "Available SDKs:"
            Write-Host $sdks
            
            # Try to use a specific SDK version if multiple are available
            $sdk8 = $sdks | Where-Object { $_ -like "*8.*" } | Select-Object -First 1
            if ($sdk8) {
                Write-Success "Found .NET 8 SDK: $sdk8"
                # Set global.json to use specific version
                $globalJson = @{ "sdk" = @{ "version" = $sdk8.Split()[0] } } | ConvertTo-Json
                $globalJson | Out-File -FilePath "global.json" -Encoding UTF8
                Write-Info "Created global.json with SDK version"
            }
        } catch {
            Write-Error "Failed to analyze .NET installation: $($_.Exception.Message)"
        }
    }
    
    return $errorDetails
}

function Handle-MSBuildError {
    param([string]$SpecificError = "")
    
    $errorDetails = Handle-Error -ErrorMessage "MSBuild error: $SpecificError" -ErrorType "MSBuildError" -Component "BuildTools" -Solution "Use 'dotnet build' instead of direct MSBuild" -IsCritical $true -WorkaroundCode @"
# Use dotnet CLI instead of MSBuild
dotnet build --configuration Release --verbosity detailed

# Alternative: Try to locate MSBuild
`$msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if (Test-Path `$msbuildPath) {
    & `$msbuildPath -version
}
"@ -RecoveryAction {
        Write-Info "Attempting MSBuild recovery..."
        
        # Try using dotnet build instead
        try {
            Write-Info "Attempting build with dotnet CLI..."
            if (Test-Path "*.sln") {
                $solutionFile = Get-ChildItem "*.sln" | Select-Object -First 1
                & dotnet build $solutionFile.Name --configuration Release --verbosity minimal
            } elseif (Test-Path "*.csproj") {
                $projectFile = Get-ChildItem "*.csproj" | Select-Object -First 1
                & dotnet build $projectFile.Name --configuration Release --verbosity minimal
            }
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Build successful with dotnet CLI"
            }
        } catch {
            Write-Error "dotnet build also failed: $($_.Exception.Message)"
        }
    }
    
    return $errorDetails
}

function Handle-PackageRestoreError {
    param([string]$SpecificError = "")
    
    $errorDetails = Handle-Error -ErrorMessage "NuGet package restore failed: $SpecificError" -ErrorType "PackageError" -Component "Dependencies" -Solution "Clear NuGet cache and retry restore" -IsCritical $true -WorkaroundCode @"
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with verbose output
dotnet restore --verbosity detailed --force

# Try with different sources
dotnet restore --source https://api.nuget.org/v3/index.json
"@ -RecoveryAction {
        Write-Info "Attempting package restore recovery..."
        
        try {
            # Clear NuGet cache
            Write-Info "Clearing NuGet cache..."
            & dotnet nuget locals all --clear
            
            # Retry restore
            Write-Info "Retrying package restore..."
            & dotnet restore --verbosity detailed --force
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Package restore successful after cache clear"
            }
        } catch {
            Write-Error "Package restore recovery failed: $($_.Exception.Message)"
            
            # Try alternative package sources
            Write-Info "Trying with explicit NuGet source..."
            & dotnet restore --source https://api.nuget.org/v3/index.json
        }
    }
    
    return $errorDetails
}

function Handle-TestFailure {
    param(
        [string]$TestName = "",
        [string]$SpecificError = ""
    )
    
    $errorDetails = Handle-Error -ErrorMessage "Test failure: $TestName - $SpecificError" -ErrorType "TestFailure" -Component "Testing" -Solution "Review test output and fix failing tests" -IsCritical $false -WorkaroundCode @"
# Run specific test with detailed output
dotnet test --logger "console;verbosity=detailed" --filter "Name~$TestName"

# Skip failing tests temporarily (not recommended for production)
dotnet test --filter "Name!~$TestName"
"@
    
    return $errorDetails
}

function Handle-AzureDeploymentError {
    param([string]$SpecificError = "")
    
    $errorDetails = Handle-Error -ErrorMessage "Azure deployment failed: $SpecificError" -ErrorType "AzureError" -Component "Deployment" -Solution "Check Azure credentials and function app configuration" -IsCritical $true -WorkaroundCode @"
# Check Azure login
az account show

# Verify function app exists
az functionapp list --query "[?name=='your-function-app-name']"

# Test deployment package
az functionapp deployment source config-zip --name your-function-app-name --resource-group your-resource-group --src package.zip
"@ -RecoveryAction {
        Write-Info "Attempting Azure deployment recovery..."
        
        try {
            # Check if Azure CLI is available
            $azVersion = & az --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Info "Azure CLI available: $azVersion"
                
                # Try to get account information
                $accountInfo = & az account show 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Azure authentication verified"
                } else {
                    Write-Warning "Azure authentication may be required"
                }
            } else {
                Write-Warning "Azure CLI not available - using publish profile method"
            }
        } catch {
            Write-Error "Azure recovery check failed: $($_.Exception.Message)"
        }
    }
    
    return $errorDetails
}

# Memory and resource monitoring
function Monitor-Resources {
    try {
        $memory = Get-CimInstance Win32_ComputerSystem
        $process = Get-Process -Id $PID
        
        $memoryUsageMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
        $totalMemoryGB = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
        
        Write-Info "💾 Current memory usage: $memoryUsageMB MB"
        Write-Info "💾 Total system memory: $totalMemoryGB GB"
        
        # Check if memory usage is high
        if ($memoryUsageMB -gt 1000) {  # More than 1GB
            Write-Warning "⚠️ High memory usage detected: $memoryUsageMB MB"
            
            # Trigger garbage collection
            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            
            $newMemoryUsage = [math]::Round((Get-Process -Id $PID).WorkingSet64 / 1MB, 2)
            Write-Info "💾 Memory usage after GC: $newMemoryUsage MB"
        }
        
        # Check disk space
        $drives = Get-CimInstance Win32_LogicalDisk | Where-Object { $_.DriveType -eq 3 }
        foreach ($drive in $drives) {
            $freeSpaceGB = [math]::Round($drive.FreeSpace / 1GB, 2)
            $totalSpaceGB = [math]::Round($drive.Size / 1GB, 2)
            $percentFree = [math]::Round(($drive.FreeSpace / $drive.Size) * 100, 1)
            
            Write-Info "💽 Drive $($drive.DeviceID) $freeSpaceGB GB free of $totalSpaceGB GB ($percentFree%)"
            
            if ($percentFree -lt 10) {
                Handle-Error -ErrorMessage "Low disk space on $($drive.DeviceID): $percentFree% free" -ErrorType "ResourceError" -Component "Storage" -Solution "Clean up temporary files or use different drive" -IsCritical $false
            }
        }
        
    } catch {
        Write-Warning "Could not monitor resources: $($_.Exception.Message)"
    }
}

# Retry mechanism
function Invoke-WithRetry {
    param(
        [scriptblock]$ScriptBlock,
        [int]$MaxRetries = $MaxRetries,
        [int]$DelaySeconds = 30,
        [string]$OperationName = "Operation"
    )
    
    for ($attempt = 1; $attempt -le $MaxRetries; $attempt++) {
        try {
            Write-Info "🔄 Attempt $attempt of $MaxRetries for $OperationName"
            $result = & $ScriptBlock
            Write-Success "✅ $OperationName completed successfully on attempt $attempt"
            return $result
        } catch {
            $errorMessage = $_.Exception.Message
            
            if ($attempt -eq $MaxRetries) {
                Handle-Error -ErrorMessage "$OperationName failed after $MaxRetries attempts: $errorMessage" -ErrorType "RetryFailure" -Component "General" -Solution "Check logs and try manual intervention" -IsCritical $true
                throw
            } else {
                Write-Warning "⚠️ Attempt $attempt failed: $errorMessage"
                Write-Info "⏳ Waiting $DelaySeconds seconds before retry..."
                Start-Sleep -Seconds $DelaySeconds
            }
        }
    }
}

# Fallback to Windows 2022 recommendation
function Suggest-Windows2022Fallback {
    if ($script:ErrorLog.System.OS -like "*Server 2025*") {
        $fallbackMessage = @"
🔄 FALLBACK RECOMMENDATION:
If you continue to experience issues on Windows Server 2025, consider:

1. Update your workflow to use 'windows-2022' temporarily:
   runs-on: windows-2022

2. Add a matrix build to test both versions:
   strategy:
     matrix:
       os: [windows-2022, windows-latest]

3. Use input parameter to choose runner:
   runs-on: `${{ github.event.inputs.runner_os || 'windows-2022' }}

This ensures stable builds while Windows Server 2025 compatibility issues are resolved.
"@
        
        Write-Warning $fallbackMessage
        
        # Add to error log
        $script:ErrorLog.Recovery["FallbackRecommended"] = $true
        $script:ErrorLog.Recovery["FallbackReason"] = "Windows Server 2025 compatibility issues"
        $script:ErrorLog.Recovery["FallbackInstructions"] = $fallbackMessage
    }
}

# Export error log
function Export-ErrorLog {
    param([string]$FilePath = $LogFile)
    
    try {
        # Add summary
        $script:ErrorLog["Summary"] = @{
            "TotalErrors" = $script:ErrorLog.Errors.Count
            "CriticalErrors" = ($script:ErrorLog.Errors | Where-Object { $_.IsCritical }).Count
            "RecoveryAttempts" = ($script:ErrorLog.Errors | Where-Object { $_.RecoveryAttempted }).Count
            "SuccessfulRecoveries" = ($script:ErrorLog.Errors | Where-Object { $_.RecoverySuccessful }).Count
        }
        
        $json = $script:ErrorLog | ConvertTo-Json -Depth 10
        $json | Out-File -FilePath $FilePath -Encoding UTF8
        
        Write-Info "📄 Error log exported to: $FilePath"
    } catch {
        Write-Error "Failed to export error log: $($_.Exception.Message)"
    }
}

# Set up error handling for the current session
$ErrorActionPreference = "Continue"  # Don't stop on errors, handle them gracefully

# Override default error handling
trap {
    $errorMessage = $_.Exception.Message
    $errorCategory = $_.CategoryInfo.Category
    $errorActivity = $_.CategoryInfo.Activity
    
    Handle-Error -ErrorMessage $errorMessage -ErrorType $errorCategory.ToString() -Component $errorActivity -IsCritical $true
    
    # Continue execution instead of stopping
    continue
}

# Export functions for use in workflows
Export-ModuleMember -Function @(
    'Handle-Error',
    'Handle-DriveNotFound',
    'Handle-SoftwareMissing',
    'Handle-DotNetError',
    'Handle-MSBuildError',
    'Handle-PackageRestoreError',
    'Handle-TestFailure',
    'Handle-AzureDeploymentError',
    'Monitor-Resources',
    'Invoke-WithRetry',
    'Suggest-Windows2022Fallback',
    'Export-ErrorLog'
)

Write-Info "🛡️ Enhanced error handling initialized for context: $ErrorContext"
Write-Info "📊 Maximum retries: $MaxRetries"
Write-Info "📄 Error log file: $LogFile"

# Monitor resources at startup
Monitor-Resources