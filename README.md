# Skkky - Medical Lab Analyzer 🏥🔬

نظام إدارة المختبرات الطبية الذكي مع دعم Windows Server 2025

[![.NET Desktop](https://github.com/you112ef/Skkky/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/you112ef/Skkky/actions/workflows/dotnet-desktop.yml)
[![Windows 2025 Compatibility](https://github.com/you112ef/Skkky/actions/workflows/windows-2025-compatibility-test.yml/badge.svg)](https://github.com/you112ef/Skkky/actions/workflows/windows-2025-compatibility-test.yml)
[![Azure Functions](https://github.com/you112ef/Skkky/actions/workflows/azure-functions-app-dotnet.yml/badge.svg)](https://github.com/you112ef/Skkky/actions/workflows/azure-functions-app-dotnet.yml)

---

## 📋 نظرة عامة

**Medical Lab Analyzer** هو نظام شامل لإدارة المختبرات الطبية، مُطور بتقنيات .NET 8.0 الحديثة مع دعم كامل للذكاء الاصطناعي وتحليل الحيوانات المنوية CASA. النظام مُحسن للعمل على Windows Server 2025 ويدعم الواجهات العربية RTL.

### ✨ المميزات الرئيسية

🔬 **تحليل CASA بالذكاء الاصطناعي**: تحليل الحيوانات المنوية باستخدام YOLOv8 و DeepSORT
🩸 **تحاليل طبية شاملة**: CBC, Urine, Stool, Glucose, Lipid Profile وأكثر
👥 **إدارة المستخدمين**: نظام صلاحيات متقدم (مدير، فني، مستقبل)
📊 **التقارير الاحترافية**: تقارير PDF و Excel قابلة للتخصيص
🌐 **واجهة عربية متقدمة**: دعم RTL كامل مع Material Design
☁️ **الدعم السحابي**: تكامل مع Azure Functions

---

## 🔧 التقنيات المستخدمة

| التقنية | الإصدار | الوصف |
|---------|---------|--------|
| .NET | 8.0 | إطار العمل الأساسي |
| WPF | 8.0 | واجهة المستخدم |
| Entity Framework | 8.0 | قاعدة البيانات |
| YOLOv8 | Latest | الذكاء الاصطناعي |
| OpenCV | 4.x | معالجة الصور |
| SQLite | 3.x | قاعدة البيانات المحلية |

---

## 🖥️ متطلبات النظام

### المتطلبات الأساسية
- **نظام التشغيل**: Windows 10/11/Server 2019/2022/2025 (64-bit)
- **.NET Runtime**: 8.0 أو أحدث
- **الذاكرة**: 4GB RAM (8GB مُستحسن)
- **المساحة**: 2GB مساحة فارغة
- **كارت الرسوميات**: مدعوم للـ AI (اختياري)

### 🆕 Windows Server 2025 Compatibility

✅ **مدعوم بالكامل** - تم اختبار التطبيق على Windows Server 2025

**المميزات الخاصة:**
- كشف تلقائي لنوع نظام التشغيل
- معالجة ذكية لتوزيع الذاكرة والمساحات
- تحسينات خاصة لبيئة الخادم
- إدارة محسنة للموارد
- دعم للنشر في بيئات المؤسسات

📚 **الأدلة والوثائق:**
- [📊 دليل التوافق الكامل](.github/COMPATIBILITY_MATRIX.md)
- [🚀 دليل الترحيل إلى Windows Server 2025](.github/MIGRATION_GUIDE.md)
- [⚙️ دليل النشر والتكوين](.github/DEPLOYMENT_GUIDE.md)

---

## 🚀 البدء السريع

### التحميل والتثبيت
```powershell
# استنساخ المستودع
git clone https://github.com/you112ef/Skkky.git
cd Skkky/MedicalLabAnalyzer

# استعادة الحزم
dotnet restore

# البناء
dotnet build --configuration Release

# التشغيل
dotnet run
```

### بيانات التجربة
| المستخدم | كلمة المرور | الصلاحية |
|----------|-------------|---------|
| admin | admin | مدير النظام |
| lab | 123 | فني المختبر |
| reception | 123 | موظف الاستقبال |

---

## 🔄 CI/CD & DevOps

### GitHub Actions Workflows

يحتوي المشروع على نظام CI/CD متقدم مع workflows مُحسّنة:

#### 🎯 [.NET Desktop Build](.github/workflows/dotnet-desktop.yml)
- بناء تلقائي متعدد المنصات
- اختبارات التوافق مع Windows Server 2025
- إنتاج artifacts جاهزة للنشر
- تشغيل الاختبارات التلقائية

#### ☁️ [Azure Functions Deployment](.github/workflows/azure-functions-app-dotnet.yml)
- نشر الخدمات السحابية تلقائياً
- validation للبيئة قبل النشر
- مراقبة صحة الخدمات بعد النشر

#### 🔧 [PowerShell Automation](.github/workflows/azure-functions-app-powershell.yml)
- أتمتة مهام الصيانة
- فحص dependencies وتحديثها
- إدارة البيئات والتكوينات

#### 📊 [Windows 2025 Compatibility Testing](.github/workflows/windows-2025-compatibility-test.yml)
- اختبارات شاملة للتوافق
- مراقبة الأداء والموارد
- تقارير مفصلة عن الاختبارات

### 🛠️ Scripts التطوير والنشر

| Script | الوصف |
|--------|-------|
| [check-dependencies.ps1](.github/scripts/check-dependencies.ps1) | فحص المتطلبات والتبعيات |
| [error-handler.ps1](.github/scripts/error-handler.ps1) | معالجة الأخطاء المتقدمة |
| [BuildDeploy.ps1](MedicalLabAnalyzer/BuildDeploy.ps1) | البناء والنشر التلقائي |

---

## 📁 هيكل المشروع

```
Skkky/
├── MedicalLabAnalyzer/           # التطبيق الرئيسي
│   ├── Models/                  # نماذج البيانات
│   ├── Services/                # الخدمات والمحللات
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Views/                   # واجهات المستخدم
│   └── AI/                      # خدمات الذكاء الاصطناعي
├── .github/
│   ├── workflows/               # GitHub Actions
│   ├── scripts/                 # PowerShell Scripts
│   ├── COMPATIBILITY_MATRIX.md  # مصفوفة التوافق
│   └── MIGRATION_GUIDE.md       # دليل الترحيل
└── docs/                        # الوثائق الفنية
```

---

## 🔬 الميزات الطبية المتقدمة

### تحليل CASA (Computer-Assisted Sperm Analysis)
- **YOLOv8**: كشف الحيوانات المنوية
- **DeepSORT**: تتبع الحركة المتقدم  
- **المقاييس**: VCL, VSL, LIN, MOT%, Count
- **دعم الملفات**: JPG, PNG, MP4, AVI
- **عمل محلي**: بدون الحاجة لإنترنت

### التحاليل المدعومة
- **الأساسية**: CBC, Urine, Stool Analysis
- **الكيميائية**: Glucose, Lipid Profile, Liver Function
- **الهرمونية**: Thyroid, Reproductive Hormones
- **المناعية**: CRP, Serology Tests
- **الميكروبيولوجية**: Culture, PCR

---

## 📊 المراقبة والتقارير

### نظام التقارير
- 📄 **PDF Reports**: تقارير احترافية قابلة للطباعة
- 📊 **Excel Export**: بيانات قابلة للتحليل
- 🖼️ **Image Integration**: إدراج الصور والرسوم البيانية
- 📱 **Mobile-Friendly**: تقارير متجاوبة

### Analytics والمراقبة
- 📈 **Performance Metrics**: مراقبة الأداء الفوري
- 🚨 **Error Tracking**: تتبع الأخطاء ومعالجتها
- 📊 **Usage Statistics**: إحصائيات الاستخدام المفصلة
- 🔒 **Audit Logs**: سجلات المراجعة الشاملة

---

## 🌐 التعريب والتدويل

### الدعم العربي المتقدم
- **RTL Layout**: تخطيط من اليمين لليسار
- **Arabic Fonts**: خطوط عربية محسنة
- **Medical Terminology**: مصطلحات طبية دقيقة
- **Cultural Adaptation**: تكيف مع البيئة المحلية

---

## 🔒 الأمان والحماية

### ميزات الأمان
- 🔐 **Data Encryption**: تشفير البيانات الحساسة
- 👤 **Role-Based Access**: التحكم بالصلاحيات
- 🔍 **Audit Trail**: تتبع جميع العمليات
- 🛡️ **HIPAA Compliance**: متوافق مع معايير الحماية الطبية

---

## 🤝 المساهمة في المشروع

نرحب بالمساهمات! يرجى قراءة [دليل المساهمة](CONTRIBUTING.md) للمزيد من التفاصيل.

### عملية التطوير
1. Fork المشروع
2. إنشاء branch للميزة الجديدة
3. Commit التغييرات
4. Push إلى branch
5. إنشاء Pull Request

---

## 📞 الدعم والتواصل

### الدعم الفني
- 🔧 **GitHub Issues**: [إبلاغ المشاكل](https://github.com/you112ef/Skkky/issues)
- 📧 **البريد الإلكتروني**: support@medicallab.com
- 📱 **الهاتف**: +966-11-1234567
- 💬 **الدعم المباشر**: [Live Chat](https://medicallab.com/support)

### موارد التطوير
- 📚 **الوثائق**: [Wiki](https://github.com/you112ef/Skkky/wiki)
- 🎥 **الفيديوهات التعليمية**: [YouTube Channel](https://youtube.com/@medicallabanalyzer)
- 💻 **نماذج الكود**: [Code Examples](examples/)

---

## 📄 الترخيص

هذا المشروع مرخص تحت [MIT License](LICENSE) - انظر ملف LICENSE للتفاصيل.

---

## 🏆 شكر خاص

شكر خاص لجميع المساهمين والمطورين الذين ساعدوا في جعل هذا المشروع حقيقة.

### المساهمون الرئيسيون
- **Team Lead**: [@you112ef](https://github.com/you112ef)
- **AI Specialist**: الفريق المتخصص في الذكاء الاصطناعي
- **Medical Consultants**: الاستشاريون الطبيون

---

## 📈 إحصائيات المشروع

![GitHub stars](https://img.shields.io/github/stars/you112ef/Skkky?style=social)
![GitHub forks](https://img.shields.io/github/forks/you112ef/Skkky?style=social)
![GitHub issues](https://img.shields.io/github/issues/you112ef/Skkky)
![GitHub pull requests](https://img.shields.io/github/issues-pr/you112ef/Skkky)

---

## 🔄 آخر التحديثات

### v2.5.0 - Windows Server 2025 Support (2025-01-17)
- ✅ دعم كامل لـ Windows Server 2025
- 🔧 تحسينات CI/CD workflows
- 📊 نظام مراقبة محسن
- 🚀 أداء محسن للخوادم

### v2.4.0 - AI Enhancements (2024-12-15)
- 🤖 تحسين خوارزميات CASA
- 📸 دعم جودة صور عالية
- ⚡ تسريع معالجة الفيديوهات

---

<div align="center">

**Medical Lab Analyzer** - حلول ذكية للمختبرات الطبية 🏥

Made with ❤️ by [you112ef](https://github.com/you112ef)

</div>