# ERP System | Beverage MES Pilot

نظام تشغيل لمصنع تعبئة مشروبات يتابع دفعات الخامات والتعبئة من الاستلام والفحص والصرف إلى الإنتاج والتعبئة والشحن. شاشة الاستدعاء تربط الدفعة بالتشغيلات والبالات والعملاء، وحجز الجودة يمنع تأكيد الشحن.

| الفرع | المحتوى |
|---|---|
| [main](https://github.com/zeadhelal579-ux/ERP-system/tree/main) | نظرة عامة، وثائق المشروع، تقرير الاختبار وسكربتات التحقق |
| [back-end](https://github.com/zeadhelal579-ux/ERP-system/tree/back-end) | ASP.NET Core وEF Core وSQL Server |
| [front-end](https://github.com/zeadhelal579-ux/ERP-system/tree/front-end) | واجهة التطوير React وTypeScript وVite |
| [front-Deploy](https://github.com/zeadhelal579-ux/ERP-system/tree/front-Deploy) | واجهة الديمو بإعدادات Vercel |

كل فرع من فروع التطبيق فيه ملفات التطبيق في جذر الفرع ودليل تشغيل مستقل. لتشغيل اختبارات التكامل، استخرج الفروع في المسارات الموضحة في [تقرير الاختبار](audit-report/REPORT.md).

## حالة الـ Pilot

- نجح 56 تحقق API باستخدام SQL Server LocalDB، و25 اختبار Vitest لكل واجهة.
- نجح تمرين الاستدعاء والحجز على الواجهتين مع الـ API الحقيقي في Chromium.
- النشر على Vercel يحتاج API منشور على HTTPS وإعداد CORS. تشغيل مصنع فعلي يحتاج تحقق من الحمل وترقية بيانات العميل والاستعادة من النسخ الاحتياطي.
- نطاق النسخة الحالية هو تتبع الدفعات والحجز والشحن. أوامر الإنتاج التفصيلية والورديات والوصفات وقياسات المعمل التفصيلية توسعات مستقبلية.

[تقرير الاختبار](audit-report/REPORT.md) · [مراجعة الأمان](audit-report/SECURITY.md)
