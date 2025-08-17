# Windows Server 2025 Compatibility Matrix
# حل مشكلة ترحيل GitHub Actions إلى Windows Server 2025

## نظرة عامة / Overview

هذا الدليل يوضح التوافق بين أنظمة التشغيل المختلفة و GitHub Actions runners لمشروع Medical Lab Analyzer، مع التركيز على حل مشكلة [GitHub Actions Issue #12677](https://github.com/actions/runner-images/issues/12677).

This guide explains compatibility between different operating systems and GitHub Actions runners for the Medical Lab Analyzer project, focusing on solving [GitHub Actions Issue #12677](https://github.com/actions/runner-images/issues/12677).

## جدول التوافق الرئيسي / Main Compatibility Matrix

| Runner Label | OS Version | .NET 8.0 | PowerShell 7.x | MSBuild | Status | Recommendation |
|--------------|------------|----------|----------------|---------|---------|----------------|
| `windows-latest` | **Windows Server 2025** (from Sep 2, 2025) | ✅ | ✅ | ✅ | **جديد/New** | **استخدم مع الحذر/Use with caution** |
| `windows-2022` | Windows Server 2022 | ✅ | ✅ | ✅ | **مستقر/Stable** | **الأفضل للإنتاج/Best for production** |
| `windows-2019` | Windows Server 2019 | ✅ | ✅ | ✅ | **قديم/Legacy** | **للتوافق فقط/Compatibility only** |

## تفاصيل التوافق / Compatibility Details

### Windows Server 2025 (windows-latest)
- **تاريخ التفعيل**: 2 سبتمبر 2025
- **التأثيرات الرئيسية**:
  - ❌ عدم توفر القرص D:\ (D:\ drive not available)
  - ⚠️ تغييرات في البرامج المثبتة (Changes in pre-installed software)
  - ✅ دعم محسن للذكاء الاصطناعي (Enhanced AI support)
  - ✅ أداء أفضل (Better performance)

### Windows Server 2022 (windows-2022)
- **الحالة**: مستقر ومختبر بشكل كامل
- **المميزات**:
  - ✅ جميع الأدوات مثبتة ومختبرة
  - ✅ توافق كامل مع .NET 8.0
  - ✅ استقرار في الإنتاج
  - ✅ القرص D:\ متوفر

### Windows Server 2019 (windows-2019)
- **الحالة**: قديم لكن مدعوم
- **التحديد**: استخدام محدود، يفضل الترقية

## مصفوفة توافق التطبيقات / Application Compatibility Matrix

### Medical Lab Analyzer Desktop Application

| Component | Windows 2019 | Windows 2022 | Windows 2025 | Notes |
|-----------|--------------|--------------|--------------|-------|
| .NET 8.0 WPF | ✅ | ✅ | ✅ | Full compatibility |
| Entity Framework | ✅ | ✅ | ✅ | SQLite works on all versions |
| OpenCV/EmguCV | ✅ | ✅ | ⚠️ | May need testing on 2025 |
| ONNX Runtime | ✅ | ✅ | ✅ | Enhanced GPU support on 2025 |
| MaterialDesign UI | ✅ | ✅ | ✅ | Full compatibility |
| Arabic RTL Support | ✅ | ✅ | ✅ | Windows font improvements |

### Azure Functions

| Runtime | Windows 2019 | Windows 2022 | Windows 2025 | Notes |
|---------|--------------|--------------|--------------|-------|
| .NET 8 Isolated | ✅ | ✅ | ✅ | Preferred runtime |
| PowerShell 7.x | ✅ | ✅ | ✅ | Enhanced on 2025 |
| Functions v4 | ✅ | ✅ | ✅ | Full support |

## إستراتيجيات التخفيف / Mitigation Strategies

### 1. استراتيجية المرونة / Flexible Runner Strategy
```yaml
# في workflows، استخدم متغير للاختيار
runs-on: ${{ github.event.inputs.runner_os || 'windows-2022' }}
```

### 2. اختبار متعدد البيئات / Multi-Environment Testing
```yaml
strategy:
  matrix:
    os: [windows-2022, windows-latest]
    include:
      - os: windows-2022
        stable: true
      - os: windows-latest
        experimental: true
```

### 3. آلية الحماية من الأخطاء / Fallback Mechanism
```yaml
jobs:
  build-stable:
    runs-on: windows-2022
  build-experimental:
    runs-on: windows-latest
    continue-on-error: true
```

## التحديات المحددة وحلولها / Specific Challenges and Solutions

### 1. مشكلة القرص D:\ غير متوفر / D:\ Drive Not Available

**المشكلة**: Windows Server 2025 لا يوفر القرص D:\

**الحل**:
```yaml
- name: Handle D drive compatibility
  shell: powershell
  run: |
    # تحقق من وجود القرص D:\
    if (Test-Path "D:\") {
      Write-Host "✅ D:\ drive available"
      $workDir = "D:\workspace"
    } else {
      Write-Host "⚠️ D:\ drive not available, using C:\"
      $workDir = "C:\workspace"
    }
    New-Item -ItemType Directory -Path $workDir -Force
    Write-Output "WORK_DIR=$workDir" >> $env:GITHUB_ENV
```

### 2. تغييرات في البرامج المثبتة / Pre-installed Software Changes

**المشكلة**: بعض البرامج قد تكون غير متوفرة أو بإصدارات مختلفة

**الحل**:
```yaml
- name: Verify and install dependencies
  shell: powershell
  run: |
    # قائمة بالبرامج المطلوبة
    $requiredSoftware = @(
      @{Name="Git"; Command="git --version"},
      @{Name="Node.js"; Command="node --version"},
      @{Name="Python"; Command="python --version"}
    )
    
    foreach ($software in $requiredSoftware) {
      try {
        $version = & { Invoke-Expression $software.Command } 2>$null
        Write-Host "✅ $($software.Name): $version"
      } catch {
        Write-Host "❌ $($software.Name) not found or not working"
        # إضافة منطق التثبيت هنا
      }
    }
```

### 3. مشاكل الذاكرة والأداء / Memory and Performance Issues

**المشكلة**: Windows Server 2025 قد يكون له خصائص أداء مختلفة

**الحل**:
```yaml
- name: Monitor system resources
  shell: powershell
  run: |
    $memory = Get-CimInstance Win32_ComputerSystem
    $disk = Get-CimInstance Win32_LogicalDisk | Where-Object {$_.DriveType -eq 3}
    
    Write-Host "💾 Total Memory: $([math]::Round($memory.TotalPhysicalMemory / 1GB, 2)) GB"
    Write-Host "💽 Available Disk Space:"
    $disk | ForEach-Object {
      $freeGB = [math]::Round($_.FreeSpace / 1GB, 2)
      Write-Host "   $($_.DeviceID) $freeGB GB free"
    }
```

## دليل الترحيل التدريجي / Gradual Migration Guide

### المرحلة 1: الإعداد والاختبار / Phase 1: Setup and Testing
1. إضافة خيار runner متغير في جميع workflows
2. اختبار workflows على `windows-latest` في بيئة التطوير
3. مراقبة النتائج وتسجيل المشاكل

### المرحلة 2: التطبيق المتدرج / Phase 2: Gradual Rollout
1. تفعيل Windows Server 2025 للبيئات غير الإنتاجية
2. اختبار شامل لجميع المميزات
3. حل المشاكل المكتشفة

### المرحلة 3: الترحيل الكامل / Phase 3: Full Migration
1. تحويل workflows الإنتاجية تدريجياً
2. مراقبة مستمرة للأداء
3. نسخ احتياطية واستراتيجية التراجع

## خطة الطوارئ / Emergency Rollback Plan

في حالة فشل Windows Server 2025:

```yaml
# استراتيجية التراجع السريع
runs-on: ${{ vars.EMERGENCY_RUNNER || 'windows-2022' }}
```

1. تحديث متغير `EMERGENCY_RUNNER` إلى `windows-2022`
2. إعادة تشغيل جميع workflows المتأثرة
3. فحص وحل المشاكل في بيئة منفصلة

## مراقبة ومتابعة / Monitoring and Tracking

### مؤشرات الأداء الرئيسية / Key Performance Indicators (KPIs)

| Metric | Windows 2022 Baseline | Windows 2025 Target | Current Status |
|--------|------------------------|---------------------|----------------|
| Build Time | 5-8 minutes | 4-7 minutes | 🟡 Monitoring |
| Success Rate | >95% | >95% | 🟡 Monitoring |
| Resource Usage | Baseline | <90% of baseline | 🟡 Monitoring |

### أدوات المراقبة / Monitoring Tools

1. **GitHub Actions Insights**: مراقبة مدة وحالة workflows
2. **Custom Monitoring**: إضافة تقارير مخصصة للأداء
3. **Alert System**: تنبيهات عند فشل builds

## أفضل الممارسات / Best Practices

### للمطورين / For Developers
1. ✅ اختبر workflows محلياً قبل النشر
2. ✅ استخدم استراتيجية المصفوفة للاختبار المتعدد
3. ✅ أضف معالجة أخطاء شاملة
4. ✅ راقب استخدام الموارد

### لفرق DevOps
1. ✅ احتفظ بنسخ احتياطية من workflows العاملة
2. ✅ اختبر في بيئات غير إنتاجية أولاً
3. ✅ راقب المؤشرات باستمرار
4. ✅ جهز خطة التراجع

### للإدارة / For Management
1. ✅ خطط للترحيل التدريجي
2. ✅ خصص وقت إضافي للاختبار
3. ✅ تواصل مع الفريق بشأن التغييرات
4. ✅ راجع النتائج دورياً

## الموارد والمراجع / Resources and References

### الوثائق الرسمية / Official Documentation
- [GitHub Actions Runner Images](https://github.com/actions/runner-images)
- [Windows Server 2025 Migration Guide](https://github.com/actions/runner-images/issues/12677)
- [.NET 8.0 Compatibility](https://docs.microsoft.com/en-us/dotnet/core/compatibility/)

### أدوات مفيدة / Useful Tools
- [Action Runner Tools](https://github.com/actions/runner)
- [Windows Compatibility Checker](https://docs.microsoft.com/en-us/windows/compatibility/)
- [Azure DevOps Migration Tools](https://azure.microsoft.com/en-us/services/devops/)

### مجتمع الدعم / Support Community
- [GitHub Community Discussions](https://github.com/orgs/community/discussions)
- [Stack Overflow - GitHub Actions](https://stackoverflow.com/questions/tagged/github-actions)
- [Reddit - r/github](https://reddit.com/r/github)

## سجل التحديثات / Update Log

| Date | Version | Changes | Impact |
|------|---------|---------|---------|
| 2025-08-17 | 1.0 | إنشاء الدليل الأولي / Initial guide creation | 🟢 Foundation |
| TBD | 1.1 | تحديثات بناء على الاختبار / Updates based on testing | 🟡 Improvements |
| TBD | 2.0 | دعم كامل لـ Windows Server 2025 / Full Windows Server 2025 support | 🟢 Production Ready |

---

## خلاصة / Summary

هذا الدليل يوفر إطار عمل شامل للتعامل مع ترحيل GitHub Actions إلى Windows Server 2025. الهدف هو ضمان استمرارية العمل مع الاستفادة من المميزات الجديدة.

This guide provides a comprehensive framework for handling GitHub Actions migration to Windows Server 2025. The goal is to ensure business continuity while taking advantage of new features.

### النقاط الرئيسية / Key Points:
1. ✅ استخدام `windows-2022` كاختيار آمن حالياً
2. ✅ اختبار تدريجي مع `windows-latest`
3. ✅ خطة طوارئ للتراجع السريع
4. ✅ مراقبة مستمرة للأداء
5. ✅ توثيق شامل للمشاكل والحلول

---

**إعداد**: Scout AI - Medical Lab Analyzer Team  
**تاريخ الإنشاء**: 17 أغسطس 2025  
**آخر تحديث**: 17 أغسطس 2025  
**الإصدار**: 1.0