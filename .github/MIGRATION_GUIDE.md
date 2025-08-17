# دليل الترحيل إلى Windows Server 2025
# Windows Server 2025 Migration Guide

## نظرة عامة / Overview

هذا الدليل يوضح كيفية التعامل مع ترحيل GitHub Actions من Windows Server 2022 إلى Windows Server 2025، مع التركيز على حل [GitHub Actions Issue #12677](https://github.com/actions/runner-images/issues/12677) لمشروع Medical Lab Analyzer.

This guide explains how to handle the migration of GitHub Actions from Windows Server 2022 to Windows Server 2025, focusing on solving [GitHub Actions Issue #12677](https://github.com/actions/runner-images/issues/12677) for the Medical Lab Analyzer project.

---

## 📅 جدول زمني للترحيل / Migration Timeline

| التاريخ / Date | الحدث / Event | التأثير / Impact |
|----------------|---------------|------------------|
| **2 سبتمبر 2025 / Sep 2, 2025** | بداية الترحيل التدريجي / Migration begins | `windows-latest` يبدأ استخدام Server 2025 / starts using Server 2025 |
| **30 سبتمبر 2025 / Sep 30, 2025** | اكتمال الترحيل / Migration complete | جميع `windows-latest` runners تستخدم Server 2025 / all runners use Server 2025 |
| **الآن / Now** | فترة الإعداد / Preparation period | **⚠️ اتخذ إجراءات الآن / Take action now** |

---

## 🚨 التغييرات الحرجة / Critical Changes

### 1. عدم توفر القرص D:\ / D:\ Drive Not Available
- **المشكلة**: Windows Server 2025 لا يوفر القرص D:\ للـ runners
- **التأثير**: فشل scripts التي تعتمد على القرص D:\
- **الحل**: استخدام القرص C:\ كبديل

### 2. تغييرات في البرامج المثبتة / Pre-installed Software Changes
- **المشكلة**: بعض البرامج قد تكون غير متوفرة أو بإصدارات مختلفة
- **التأثير**: فشل في builds أو tests
- **الحل**: فحص وتثبيت البرامج المطلوبة صراحة

### 3. تحسينات الأداء / Performance Improvements
- **المشكلة**: تغييرات في استهلاك الذاكرة والمعالج
- **التأثير**: اختلاف في أوقات البناء
- **الحل**: مراقبة ومعايرة الأداء

---

## 🛠️ خطة الترحيل التدريجي / Gradual Migration Plan

### المرحلة 1: الإعداد والتحضير / Phase 1: Preparation (الآن / Now)

#### 1.1 تحديث Workflows
```yaml
# إضافة خيار اختيار runner
workflow_dispatch:
  inputs:
    runner_os:
      description: 'Choose Windows runner version'
      required: false
      default: 'windows-2022'  # آمن حالياً / Currently safe
      type: choice
      options:
        - windows-latest
        - windows-2022
        - windows-2019

jobs:
  build:
    runs-on: ${{ github.event.inputs.runner_os || 'windows-2022' }}
```

#### 1.2 إضافة فحص التوافق
```yaml
- name: Check Windows Server 2025 compatibility
  shell: powershell
  run: |
    $os = (Get-CimInstance Win32_OperatingSystem).Caption
    Write-Host "Running on: $os"
    
    if ($os -like "*Server 2025*") {
      Write-Host "🔄 Windows Server 2025 detected"
      Write-Host "::set-output name=is-server-2025::true"
      
      # فحص القرص D:\ / Check D:\ drive
      if (!(Test-Path "D:\")) {
        Write-Warning "D:\ drive not available - using C:\ workspace"
        New-Item -ItemType Directory -Path "C:\workspace" -Force
        Write-Host "::set-env name=WORKSPACE_DIR::C:\workspace"
      }
    } else {
      Write-Host "::set-output name=is-server-2025::false"
      Write-Host "::set-env name=WORKSPACE_DIR::D:\workspace"
    }
```

#### 1.3 تفعيل فحوصات التبعيات
```yaml
- name: Check dependencies
  shell: powershell
  run: |
    # تشغيل سكريبت فحص التبعيات
    .\.github\scripts\check-dependencies.ps1 -ProjectType desktop -Detailed -ExportReport
```

### المرحلة 2: الاختبار التدريجي / Phase 2: Gradual Testing

#### 2.1 اختبار البيئات غير الإنتاجية
```yaml
# اختبار على كل من النظامين
strategy:
  matrix:
    os: [windows-2022, windows-latest]
    include:
      - os: windows-2022
        stable: true
        experimental: false
      - os: windows-latest
        stable: false
        experimental: true
  fail-fast: false  # استمر حتى لو فشل أحدهما

jobs:
  build:
    runs-on: ${{ matrix.os }}
    continue-on-error: ${{ matrix.experimental }}
```

#### 2.2 مراقبة النتائج
```yaml
- name: Monitor build performance
  shell: powershell
  run: |
    # قياس أوقات البناء
    $buildStart = Get-Date
    # ... عملية البناء
    $buildEnd = Get-Date
    $duration = ($buildEnd - $buildStart).TotalMinutes
    
    Write-Host "Build Duration: $duration minutes"
    Write-Host "::set-output name=build-duration::$duration"
    
    # مقارنة مع baseline
    if ($duration -gt 10) {  # إذا تجاوز 10 دقائق
      Write-Warning "Build took longer than expected: $duration minutes"
    }
```

### المرحلة 3: الترحيل الكامل / Phase 3: Full Migration

#### 3.1 تحديث الـ Production Workflows
```yaml
# استراتيجية التدرج
jobs:
  preflight:
    runs-on: windows-latest
    outputs:
      should-use-2025: ${{ steps.check.outputs.ready }}
    steps:
      - id: check
        run: |
          # فحص إذا كانت جميع التبعيات جاهزة
          if (Test-Dependencies) {
            Write-Host "::set-output name=ready::true"
          } else {
            Write-Host "::set-output name=ready::false"
          }

  build:
    needs: preflight
    runs-on: ${{ needs.preflight.outputs.should-use-2025 == 'true' && 'windows-latest' || 'windows-2022' }}
```

---

## 🔧 حلول تقنية محددة / Specific Technical Solutions

### حل مشكلة القرص D:\ / D:\ Drive Solution

#### الطريقة 1: Dynamic Path Selection
```powershell
function Get-WorkspaceDirectory {
    if (Test-Path "D:\") {
        return "D:\workspace"
    } else {
        Write-Warning "D:\ not available, using C:\workspace"
        return "C:\workspace"
    }
}

$workspaceDir = Get-WorkspaceDirectory
New-Item -ItemType Directory -Path $workspaceDir -Force | Out-Null
Write-Output "WORKSPACE_DIR=$workspaceDir" >> $env:GITHUB_ENV
```

#### الطريقة 2: Environment Variable Override
```yaml
env:
  WORKSPACE_DIR: ${{ runner.os == 'Windows' && runner.name == 'windows-latest' && 'C:\workspace' || 'D:\workspace' }}
```

### حل مشاكل البناء / Build Issues Solutions

#### مشكلة .NET SDK
```yaml
- name: Setup .NET with fallback
  shell: powershell
  run: |
    try {
      # محاولة استخدام setup-dotnet action
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
    } catch {
      # تنزيل وتثبيت يدوي
      $url = "https://download.visualstudio.microsoft.com/download/pr/.../dotnet-sdk-8.0-win-x64.exe"
      $installer = "$env:TEMP\dotnet-installer.exe"
      Invoke-WebRequest -Uri $url -OutFile $installer
      Start-Process $installer -ArgumentList "/quiet" -Wait
    }
```

#### مشكلة MSBuild
```yaml
- name: Setup MSBuild with fallback
  shell: powershell
  run: |
    # محاولة العثور على MSBuild
    $msbuildPaths = @(
      "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
      "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    $msbuildFound = $false
    foreach ($path in $msbuildPaths) {
      if (Test-Path $path) {
        Write-Host "Found MSBuild: $path"
        Write-Output "MSBUILD_PATH=$path" >> $env:GITHUB_ENV
        $msbuildFound = $true
        break
      }
    }
    
    if (!$msbuildFound) {
      Write-Host "MSBuild not found, will use 'dotnet build'"
      Write-Output "USE_DOTNET_BUILD=true" >> $env:GITHUB_ENV
    }
```

### حل مشاكل Azure Functions / Azure Functions Solutions

#### تحديث host.json للتوافق
```json
{
  "version": "2.0",
  "functionTimeout": "00:10:00",
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  },
  "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[4.*, 5.0.0)"
  },
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true
      }
    }
  }
}
```

#### PowerShell Functions التوافق
```yaml
- name: Setup PowerShell Functions environment
  shell: powershell
  run: |
    # التأكد من PowerShell 7.x
    if ($PSVersionTable.PSVersion.Major -lt 7) {
      Write-Warning "PowerShell 7+ recommended for Azure Functions"
    }
    
    # تثبيت الوحدات المطلوبة
    $modules = @('Az.Accounts', 'Az.Functions', 'Pester')
    foreach ($module in $modules) {
      if (!(Get-Module -ListAvailable -Name $module)) {
        Install-Module -Name $module -Force -AllowClobber
      }
    }
```

---

## 🧪 اختبار التوافق / Compatibility Testing

### اختبار شامل / Comprehensive Testing

#### سكريبت اختبار التوافق
```powershell
# test-windows2025-compatibility.ps1
param([switch]$DetailedReport)

Write-Host "🧪 اختبار التوافق مع Windows Server 2025"

# اختبار 1: فحص نظام التشغيل
$os = (Get-CimInstance Win32_OperatingSystem).Caption
$isServer2025 = $os -like "*Server 2025*"

Write-Host "نظام التشغيل: $os"
Write-Host "Server 2025: $isServer2025"

# اختبار 2: فحص الأقراص المتاحة
Write-Host "`n💽 فحص الأقراص:"
$drives = Get-WmiObject -Class Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
foreach ($drive in $drives) {
    $freeGB = [math]::Round($drive.FreeSpace / 1GB, 2)
    Write-Host "  $($drive.DeviceID) - $freeGB GB متاح"
}

if (!(Test-Path "D:\")) {
    Write-Warning "⚠️ القرص D:\ غير متاح"
    $script:Issues += "D drive not available"
}

# اختبار 3: فحص البرامج الأساسية
Write-Host "`n🔧 فحص البرامج:"
$software = @{
    ".NET SDK" = { dotnet --version }
    "Git" = { git --version }
    "PowerShell" = { $PSVersionTable.PSVersion }
    "MSBuild" = { Get-Command msbuild -ErrorAction SilentlyContinue }
}

foreach ($name in $software.Keys) {
    try {
        $version = & $software[$name] 2>$null
        Write-Host "  ✅ $name`: $version"
    } catch {
        Write-Host "  ❌ $name`: غير متاح"
        $script:Issues += "$name not available"
    }
}

# اختبار 4: فحص الذاكرة والأداء
Write-Host "`n💾 فحص الموارد:"
$memory = Get-CimInstance Win32_ComputerSystem
$totalGB = [math]::Round($memory.TotalPhysicalMemory / 1GB, 2)
Write-Host "  إجمالي الذاكرة: $totalGB GB"

# اختبار 5: فحص المتطلبات الخاصة بالمشروع
Write-Host "`n🏥 فحص متطلبات Medical Lab Analyzer:"

# فحص ملف المشروع
if (Test-Path "MedicalLabAnalyzer/MedicalLabAnalyzer.csproj") {
    Write-Host "  ✅ ملف المشروع موجود"
} else {
    Write-Host "  ❌ ملف المشروع غير موجود"
}

# فحص AI dependencies
$aiPaths = @(
    "MedicalLabAnalyzer/AI/YOLOv8",
    "MedicalLabAnalyzer/AI/DeepSORT",
    "MedicalLabAnalyzer/AI/Config"
)

foreach ($path in $aiPaths) {
    if (Test-Path $path) {
        Write-Host "  ✅ مجلد AI: $path"
    } else {
        Write-Host "  ⚠️ مجلد AI مفقود: $path"
    }
}

# النتيجة النهائية
Write-Host "`n📊 ملخص النتائج:"
if ($script:Issues.Count -eq 0) {
    Write-Host "  🎉 جميع الاختبارات نجحت! النظام متوافق."
    exit 0
} else {
    Write-Host "  ⚠️ وجدت $($script:Issues.Count) مشكلة:"
    foreach ($issue in $script:Issues) {
        Write-Host "    • $issue"
    }
    exit 1
}
```

### دمج الاختبار في Workflow
```yaml
- name: Run Windows 2025 compatibility test
  shell: powershell
  run: |
    .\.github\scripts\test-windows2025-compatibility.ps1 -DetailedReport
  continue-on-error: false  # فشل البناء إذا فشل الاختبار
```

---

## 🚦 استراتيجيات التخفيف / Mitigation Strategies

### 1. استراتيجية التدرج المرحلي / Phased Rollout Strategy

```yaml
name: Phased Windows 2025 Migration

on:
  workflow_dispatch:
    inputs:
      migration_phase:
        description: 'Migration phase'
        required: true
        default: 'phase1'
        type: choice
        options:
          - phase1  # Testing only
          - phase2  # Partial migration
          - phase3  # Full migration

env:
  # تحديد runner بناءً على المرحلة
  RUNNER_OS: >-
    ${{
      github.event.inputs.migration_phase == 'phase1' && 'windows-2022' ||
      github.event.inputs.migration_phase == 'phase2' && 
        (github.ref == 'refs/heads/main' && 'windows-2022' || 'windows-latest') ||
      github.event.inputs.migration_phase == 'phase3' && 'windows-latest' ||
      'windows-2022'
    }}

jobs:
  build:
    runs-on: ${{ env.RUNNER_OS }}
```

### 2. استراتيجية الاحتياط / Backup Strategy

```yaml
jobs:
  primary-build:
    runs-on: windows-latest
    outputs:
      success: ${{ steps.build.outcome == 'success' }}
    steps:
      - id: build
        continue-on-error: true
        run: |
          # محاولة البناء على Windows 2025
          # Build attempt on Windows 2025

  fallback-build:
    runs-on: windows-2022
    needs: primary-build
    if: needs.primary-build.outputs.success != 'true'
    steps:
      - run: |
          Write-Host "🔄 Primary build failed, using Windows 2022 fallback"
          # إجراء البناء الاحتياطي
          # Perform fallback build
```

### 3. استراتيجية المراقبة المستمرة / Continuous Monitoring Strategy

```yaml
- name: Performance monitoring
  shell: powershell
  run: |
    # مراقبة الأداء والموارد
    $startTime = Get-Date
    
    # قياس استهلاك الذاكرة
    $process = Get-Process -Id $PID
    $initialMemory = $process.WorkingSet64 / 1MB
    
    Write-Host "بدء العملية: $(Get-Date -Format 'HH:mm:ss')"
    Write-Host "استهلاك الذاكرة الأولي: $initialMemory MB"
    
    # ... عملية البناء
    
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMinutes
    $finalMemory = (Get-Process -Id $PID).WorkingSet64 / 1MB
    
    Write-Host "انتهاء العملية: $(Get-Date -Format 'HH:mm:ss')"
    Write-Host "المدة الإجمالية: $duration دقيقة"
    Write-Host "استهلاك الذاكرة النهائي: $finalMemory MB"
    
    # إرسال المقاييس إلى مراقبة
    Write-Output "::set-output name=duration::$duration"
    Write-Output "::set-output name=memory-usage::$finalMemory"
```

---

## 📋 قائمة مراجعة الترحيل / Migration Checklist

### قبل الترحيل / Pre-Migration
- [ ] ✅ تحديث جميع workflows لتدعم اختيار runner
- [ ] ✅ إضافة فحوصات التوافق
- [ ] ✅ تحديث scripts لتتعامل مع عدم توفر القرص D:\
- [ ] ✅ اختبار workflows على بيئات التطوير
- [ ] ✅ إنشاء نسخ احتياطية من workflows العاملة
- [ ] ✅ توثيق المشاكل المتوقعة والحلول
- [ ] ✅ إعداد مراقبة الأداء
- [ ] ✅ تدريب الفريق على الحلول الجديدة

### أثناء الترحيل / During Migration
- [ ] 🔄 مراقبة builds في الوقت الفعلي
- [ ] 🔄 اختبار جميع workflows الحرجة
- [ ] 🔄 فحص الأداء والموارد
- [ ] 🔄 توثيق المشاكل الجديدة
- [ ] 🔄 تطبيق الحلول السريعة
- [ ] 🔄 تحديث الفريق بانتظام
- [ ] 🔄 التحضير للتراجع إذا لزم الأمر

### بعد الترحيل / Post-Migration
- [ ] ⏳ مراجعة جميع النتائج
- [ ] ⏳ توثيق الدروس المستفادة
- [ ] ⏳ تحديث الوثائق
- [ ] ⏳ تحسين الأداء
- [ ] ⏳ حذف الكود القديم غير المستخدم
- [ ] ⏳ تحديث استراتيجية المراقبة
- [ ] ⏳ تقييم نجاح الترحيل

---

## 🆘 خطة الطوارئ / Emergency Plan

### التراجع السريع / Quick Rollback

#### 1. التراجع عبر GitHub Variables
```yaml
# إضافة متغير repository
# Repository Settings > Secrets and variables > Actions > Variables
EMERGENCY_RUNNER: windows-2022

# في workflows:
runs-on: ${{ vars.EMERGENCY_RUNNER || github.event.inputs.runner_os || 'windows-latest' }}
```

#### 2. التراجع عبر Feature Flag
```yaml
env:
  USE_WINDOWS_2025: ${{ vars.ENABLE_WINDOWS_2025 != 'false' }}

jobs:
  build:
    runs-on: ${{ env.USE_WINDOWS_2025 == 'true' && 'windows-latest' || 'windows-2022' }}
```

#### 3. التراجع الفوري / Immediate Rollback
```powershell
# سكريبت تراجع سريع
# emergency-rollback.ps1

Write-Host "🚨 تنفيذ التراجع الطارئ"

# تحديث جميع workflows
$workflows = Get-ChildItem ".github/workflows" -Filter "*.yml"
foreach ($workflow in $workflows) {
    $content = Get-Content $workflow.FullName -Raw
    $content = $content -replace 'windows-latest', 'windows-2022'
    Set-Content $workflow.FullName $content
    Write-Host "✅ تم تحديث: $($workflow.Name)"
}

Write-Host "🎯 تراجع مكتمل - commit and push الآن!"
```

### إجراءات الطوارئ / Emergency Procedures

#### عند فشل البناء / Build Failure
1. **تحديد المشكلة فوراً**
   ```yaml
   - name: Emergency diagnosis
     if: failure()
     shell: powershell
     run: |
       Write-Host "🚨 Build failed - running emergency diagnosis"
       
       # فحص سريع للنظام
       Get-CimInstance Win32_OperatingSystem | Select Caption, Version
       
       # فحص الأخطاء الشائعة
       if (!(Test-Path "D:\")) {
         Write-Host "❌ D:\ drive missing - Windows 2025 issue"
       }
       
       # فحص البرامج الأساسية
       try { dotnet --version } catch { Write-Host "❌ .NET issue" }
       try { git --version } catch { Write-Host "❌ Git issue" }
   ```

2. **تفعيل التراجع التلقائي**
   ```yaml
   - name: Auto-rollback trigger
     if: failure() && contains(matrix.os, 'windows-latest')
     run: |
       Write-Host "🔄 Triggering auto-rollback to windows-2022"
       # إرسال إشعار للفريق
       # Notify team
   ```

#### عند مشاكل الأداء / Performance Issues
1. **مراقبة فورية**
2. **مقارنة مع baseline**
3. **اتخاذ قرار التراجع**

---

## 📞 الدعم والتواصل / Support and Communication

### جهات الاتصال / Contact Points

#### الفريق التقني / Technical Team
- **مدير المشروع**: تطوير Medical Lab Analyzer
- **مهندس DevOps**: إدارة CI/CD
- **مطور النظام**: حل المشاكل التقنية

#### خطة التواصل / Communication Plan
1. **إشعار مسبق**: قبل 48 ساعة من الترحيل
2. **تحديثات منتظمة**: كل 6 ساعات أثناء الترحيل
3. **تقرير نهائي**: خلال 24 ساعة من انتهاء الترحيل

### الموارد والمراجع / Resources and References

#### الوثائق الرسمية / Official Documentation
- [GitHub Actions Runner Images](https://github.com/actions/runner-images)
- [Windows Server 2025 Issue #12677](https://github.com/actions/runner-images/issues/12677)
- [.NET 8.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)

#### الأدوات المساعدة / Helper Tools
- [Dependency Check Script](./scripts/check-dependencies.ps1)
- [Error Handler](./scripts/error-handler.ps1)
- [Compatibility Matrix](./COMPATIBILITY_MATRIX.md)

---

## 📈 مؤشرات النجاح / Success Metrics

### مؤشرات الأداء الرئيسية / Key Performance Indicators

| المؤشر / Metric | القيمة الحالية / Current | الهدف / Target | الحالة / Status |
|------------------|------------------------|-----------------|------------------|
| معدل نجاح البناء / Build Success Rate | 95% | >95% | 🟢 |
| متوسط وقت البناء / Average Build Time | 6 دقائق / min | <7 دقائق / min | 🟢 |
| استهلاك الموارد / Resource Usage | Baseline | <110% of baseline | 🟡 |
| عدد المشاكل / Issues Count | 0 | 0 | 🟢 |

### تقييم النجاح / Success Evaluation

#### ✅ الترحيل ناجح إذا / Migration successful if:
- جميع builds تعمل بنجاح
- الأداء ضمن المعدل المتوقع
- لا توجد مشاكل حرجة جديدة
- الفريق مريح مع النظام الجديد

#### ⚠️ الترحيل يحتاج تحسين إذا / Migration needs improvement if:
- بعض builds تفشل أحياناً
- الأداء أبطأ من المتوقع
- هناك مشاكل بسيطة لكن قابلة للحل

#### ❌ الترحيل فاشل إذا / Migration failed if:
- معظم builds تفشل
- المشاكل الحرجة لا يمكن حلها
- الأداء سيء جداً
- الفريق لا يستطيع العمل بكفاءة

---

## 🎯 الخلاصة والتوصيات / Summary and Recommendations

### الخلاصة / Summary
هذا الدليل يوفر خطة شاملة للترحيل من Windows Server 2022 إلى Windows Server 2025 لمشروع Medical Lab Analyzer. الهدف هو ضمان الانتقال السلس مع الحفاظ على الاستقرار والأداء.

This guide provides a comprehensive plan for migrating from Windows Server 2022 to Windows Server 2025 for the Medical Lab Analyzer project. The goal is to ensure smooth transition while maintaining stability and performance.

### التوصيات النهائية / Final Recommendations

#### للمطورين / For Developers
1. ✅ **ابدأ بالتحضير الآن** - لا تنتظر سبتمبر
2. ✅ **اختبر workflows على windows-latest** في بيئات التطوير
3. ✅ **حدث scripts لتدعم عدم توفر القرص D:\**
4. ✅ **أضف معالجة أخطاء شاملة**

#### لفرق DevOps
1. ✅ **راقب الأداء باستمرار**
2. ✅ **جهز خطط التراجع**
3. ✅ **وثق جميع التغييرات**
4. ✅ **درب الفريق على الإجراءات الجديدة**

#### للإدارة / For Management
1. ✅ **خصص وقت كافي للترحيل**
2. ✅ **وفر الموارد اللازمة**
3. ✅ **تواصل مع الفريق بانتظام**
4. ✅ **راجع النتائج وقيم النجاح**

### النظرة المستقبلية / Future Outlook
Windows Server 2025 يجلب تحسينات في الأداء والأمان، وخاصة في دعم الذكاء الاصطناعي. رغم التحديات الأولية، الترحيل المخطط له بعناية سيحسن من كفاءة النظام على المدى الطويل.

Windows Server 2025 brings improvements in performance and security, especially in AI support. Despite initial challenges, a carefully planned migration will improve system efficiency in the long term.

---

**إعداد**: Scout AI - Medical Lab Analyzer Team  
**تاريخ الإنشاء**: 17 أغسطس 2025  
**آخر تحديث**: 17 أغسطس 2025  
**الإصدار**: 1.0

---

> 💡 **نصيحة مهمة**: هذا الدليل مستند حي يجب تحديثه بناءً على التجارب والنتائج الفعلية. شارك تجربتك مع الفريق لتحسين العملية للجميع.
>
> **Important Note**: This is a living document that should be updated based on actual experiences and results. Share your experience with the team to improve the process for everyone.

---

## 📖 فهرس سريع / Quick Index

- [📅 الجدول الزمني](#-جدول-زمني-للترحيل--migration-timeline)
- [🚨 التغييرات الحرجة](#-التغييرات-الحرجة--critical-changes)
- [🛠️ خطة الترحيل](#️-خطة-الترحيل-التدريجي--gradual-migration-plan)
- [🔧 الحلول التقنية](#-حلول-تقنية-محددة--specific-technical-solutions)
- [🧪 اختبار التوافق](#-اختبار-التوافق--compatibility-testing)
- [🚦 استراتيجيات التخفيف](#-استراتيجيات-التخفيف--mitigation-strategies)
- [📋 قائمة المراجعة](#-قائمة-مراجعة-الترحيل--migration-checklist)
- [🆘 خطة الطوارئ](#-خطة-الطوارئ--emergency-plan)
- [📈 مؤشرات النجاح](#-مؤشرات-النجاح--success-metrics)