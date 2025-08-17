# Medical Lab Analyzer

نظام تحليل المختبرات الطبية المتقدم مع دعم الذكاء الاصطناعي

## المميزات الرئيسية

### 🏥 إدارة شاملة للمختبر
- نظام صلاحيات ثلاثي المستويات (مدير، فني مختبر، مستقبل)
- إدارة المرضى مع الصور الشخصية
- إدارة الفحوصات والنتائج
- نظام مراجعة شامل للعمليات

### 🧪 تحاليل طبية متنوعة
#### التحاليل الأساسية:
- **CASA**: تحليل الحيوانات المنوية بالذكاء الاصطناعي
- **CBC**: تعداد الدم الكامل
- **Urine**: تحليل البول
- **Stool**: تحليل البراز

#### التحاليل المتقدمة:
- **Glucose**: السكر في الدم
- **Lipid Profile**: دهون الدم
- **Liver Function**: وظائف الكبد
- **Kidney Function**: وظائف الكلى
- **CRP**: البروتين التفاعلي
- **Thyroid**: الغدة الدرقية
- **Electrolytes**: الأملاح
- **Coagulation**: التجلط
- **Vitamin**: الفيتامينات
- **Hormones**: الهرمونات
- **Microbiology**: الميكروبيولوجي
- **PCR**: تفاعل البوليمراز المتسلسل
- **Serology**: الأمصال

### 🤖 تحليل ذكي متقدم
- **YOLOv8**: كشف وتتبع الحيوانات المنوية
- **DeepSORT**: تتبع الحركة المتقدم
- **مقاييس CASA الكاملة**: VCL, VSL, LIN, MOT%, Count
- معالجة الصور: JPG, PNG
- معالجة الفيديو: MP4, AVI
- عمل محلي 100% بدون إنترنت

### 📊 نظام التقارير
- تقارير PDF احترافية
- تقارير Excel قابلة للتخصيص
- إدراج الصور في التقارير
- قوالب متعددة

### 🌐 واجهة عربية متقدمة
- دعم RTL كامل
- خطوط عربية محسنة
- تصميم طبي احترافي
- Material Design

### 💾 قاعدة بيانات محلية
- SQLite للأداء العالي
- نسخ احتياطي تلقائي
- تشفير البيانات الحساسة
- استرداد سريع

## متطلبات النظام

### بيئة التطوير والنشر
- Windows 10/11/Server 2019/2022/2025 (64-bit)
- .NET 8.0 Runtime
- 4GB RAM (8GB مُستحسن)
- 2GB مساحة فارغة
- كارت رسوميات لتسريع الذكاء الاصطناعي (اختياري)

### توافق Windows Server 2025
✅ **مدعوم بالكامل** - تم اختبار التطبيق على Windows Server 2025

- دعم تلقائي للكشف عن نوع نظام التشغيل
- معالجة ذكية لتوزيع الذاكرة والمساحات
- تحسينات خاصة لبيئة الخادم
- إدارة محسنة للموارد

📋 [دليل التوافق الكامل](.github/COMPATIBILITY_MATRIX.md)
📚 [دليل الترحيل إلى Windows Server 2025](.github/MIGRATION_GUIDE.md)

## التثبيت والتشغيل

1. استخرج ملفات البرنامج
2. شغل `MedicalLabAnalyzer.exe`
3. استخدم بيانات المستخدمين التجريبيين:

| المستخدم | كلمة المرور | الدور |
|----------|-------------|-------|
| admin | admin | المدير |
| lab | 123 | فني المختبر |
| reception | 123 | المستقبل |

## البناء من المصدر

```powershell
# استعادة الحزم
dotnet restore

# البناء
dotnet build --configuration Release

# النشر
dotnet publish -c Release -r win-x64 --self-contained true
```

أو استخدم سكربت البناء التلقائي:
```powershell
.\BuildDeploy.ps1
```

## الهيكل التقني

### تقنيات التطبيق
- **إطار العمل**: .NET 8.0 + WPF
- **قاعدة البيانات**: SQLite + Entity Framework
- **الذكاء الاصطناعي**: ONNX Runtime + YOLOv8
- **معالجة الصور**: OpenCV + EmguCV
- **التقارير**: QuestPDF + EPPlus
- **الواجهة**: Material Design + RTL

### DevOps و CI/CD
- **GitHub Actions**: بناء ونشر تلقائي
- **Azure Functions**: دعم الخدمات السحابية
- **PowerShell Scripts**: أتمتة البناء والنشر
- **Automated Testing**: اختبارات تلقائية شاملة
- **Windows Server 2025**: توافق متقدم

### أدوات التطوير
- **Visual Studio 2022**
- **Docker Support**
- **Performance Monitoring**
- **Error Tracking & Analytics**

## متابعة التطوير والنشر

### GitHub Actions Workflows
يحتوي المشروع على workflows متقدمة للبناء والنشر:

- **🎯 .NET Desktop Build**: بناء تلقائي مع اختبار التوافق
- **☁️ Azure Functions Deployment**: نشر الخدمات السحابية
- **📊 Windows 2025 Compatibility**: اختبارات توافق شاملة
- **🔎 PowerShell Automation**: أتمتة مهام الصيانة

### مراقبة البناء
- تقارير تلقائية عن حالة البناء
- مراقبة الأداء مع إنذارات مبكرة
- نسخ احتياطي تلقائي لقاعدة البيانات
- تحليلات استخدام مفصلة

### موارد التطوير
📁 **الوثائق التقنية**:
- [GitHub Actions Workflows](.github/workflows/)
- [PowerShell Scripts](.github/scripts/)
- [Compatibility Matrix](.github/COMPATIBILITY_MATRIX.md)
- [Migration Guide](.github/MIGRATION_GUIDE.md)

🎨 **موارد التصميم**:
- [UI/UX Guidelines](docs/design/)
- [Medical Icons Pack](assets/icons/)
- [RTL Layout Templates](resources/rtl/)

## الدعم الفني

لأي استفسارات أو مشاكل تقنية، يرجى التواصل عبر:

**📞 الدعم العاجل**: +966-11-1234567
**📧 الدعم الفني**: support@medicallab.com
**👾 بلاغ المشاكل**: [GitHub Issues](https://github.com/you112ef/Skkky/issues)
**📚 الوثائق**: [Wiki](https://github.com/you112ef/Skkky/wiki)

---
© 2025 Medical Lab Analyzer. جميع الحقوق محفوظة.