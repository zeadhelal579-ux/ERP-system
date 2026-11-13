# تقرير اختبار Beverage MES Pilot

**13 نوفمبر 2026 · Full، تغطية مختلطة · Windows / SQL Server LocalDB / Chromium**

**الديمو: نجح محليًا. جاهزية تشغيل مصنع: بدون حكم.** تمرين تتبع دفعة الأغطية والحجز اتنفذ على الواجهتين مع قاعدة حقيقية وبيانات اختبار. اختبار الحمل والنشر الفعلي واستعادة النسخ الاحتياطية وترقية نسخة من بيانات العميل ما اتنفذوش.

## ملخص التنفيذ

- بناء Backend وFrontend ونسخة Vercel نجح. Lint و25 Vitest نجحوا لكل واجهة.
- 56 تحقق API على قاعدة مؤقتة، منها صلاحيات الأدوار، صرف متكرر، منع استخدام نفس حركة الصرف مرتين، وشحنة متعددة التشغيلات. الـ Recall رجع تشغيلتين و200 غطاء متبقيين، وبالتين خرجوا لنفس العميل، وبالة محجوزة. محاولة شحن المجموعة بعد الحجز اترفضت بالكامل.
- تجربتا Chromium على الواجهة الأساسية ونسخة الديمو مع الـ API الحقيقي. الدخول، الاستدعاء، فك الحجز وإعادته نجحوا. فحص axe سجّل صفر مخالفات في الشاشة اللي اتفحصت. [نتيجة المتصفح](logs/browser-results.json) · [سجل السيناريو](logs/browser-scenario.log)
- فحص الأمان مفصل في [SECURITY.md](SECURITY.md). NuGet وnpm runtime سجلوا صفر تنبيهات. أدوات lint فيها 6 تنبيهات Moderate مسجلة في L07-001.

## الطبقات

| # | الطبقة | الحالة | التغطية |
|---|---|---|---|
| 1 | Static Analysis | Passed | بناء .NET وواجهتي React وفحص lint اكتملوا. |
| 2 | Unit Tests | Partial | 25 اختبار Vitest في كل نسخة. لا توجد مجموعة unit للـ Backend. |
| 3 | Database | Partial | EF model مطابق للـ snapshot، وسلسلة migrations اشتغلت على SQL Server LocalDB جديدة. ترقية بيانات فعلية وBackup/Restore خارج التحقق. |
| 4 | Integration / API | Partial | تمرين Test-Recall.ps1 على API حقيقي. تغطية السيناريو والأدوار المرتبطة به. جرد المسارات في logs/route-inventory.txt. |
| 5 | Contracts | Partial | الواجهتان اتبنتا، واختبار API تحقق من الحقول الأساسية للدفعة والصرف والشحن. لم تُقارن كل مخططات OpenAPI آليًا. |
| 6 | Authorization | Partial | HTTP 401 للضيف، 403 للأدوار غير المصرح لها في مسارات الإنتاج والحجز والشحن والاستدعاء. مصفوفة كل المسارات غير مكتملة. |
| 7 | Defensive Security | Partial | فحص NuGet وnpm ومراجعة الكود. راجع SECURITY.md للضوابط والنتائج. |
| 8 | Business Logic | Partial | تم التحقق من تتبع تشغيلتين، مخزون 200 غطاء، بالات شُحنت وحجز يمنع الشحن الذري. الدفع غير منطبق. |
| 9 | End to End | Partial | سجل API كامل وتجربتا Chromium حقيقيتان لتسجيل الدخول والاستدعاء ورفع وإعادة الحجز. مسار الشحن في المتصفح لم يُغطّ بالكامل. |
| 10 | Cross-browser | Partial | Chromium على 1440×900 و390×844 للواجهتين. Firefox وWebKit وأجهزة المصنع لم تُختبر. |
| 11 | Accessibility | Partial | axe على شاشة الاستدعاء في النسختين سجل صفر مخالفات. باقي الشاشات والتدقيق اليدوي خارج التغطية. |
| 12 | Visual Regression | Skipped | input: لا توجد صورة أساس معتمدة للمقارنة. |
| 13 | Performance and Load | Skipped | input: لا يوجد هدف حمولة متفق عليه، ولم يُنفذ اختبار حمل. |
| 14 | Resilience | Partial | إعادة إرسال الصرف وبدء التشغيلة والشحن مغطاة. انقطاع قاعدة البيانات وتعافي التكاملات غير مغطى. |
| 15 | Deployment Readiness | Partial | نجح بناء نسخة Vercel باستخدام عنوان HTTPS تجريبي. مطلوب عنوان API حقيقي وCORS وبيئة HTTPS للنشر. |

## المتبقي قبل تشغيل مصنع

1. إجراء ترقية على نسخة بيانات العميل، مع تسوية السجلات التاريخية وتجربة Backup/Restore. اتعدّل قيد في أول migration لأنه كان يوقف إنشاء قاعدة جديدة على SQL Server. احتفظنا بمعرّفات الـ migrations.
2. ضبط عنوان API HTTPS وCORS في بيئة Vercel، ومراجعة حساب الخدمة والـ secrets وسياسة الدخول والتعافي.
3. تجربة الحمل والمتصفحات والأجهزة المستهدفة. التحقق من طباعة الملصقات والربط الصناعي وERP خارج المرحلة المنفذة.
4. استكمال نموذج أوامر الإنتاج والوردية والوصفات ومواقع المخزون وقياسات المعمل الرقمية. الكميات في الديمو تتبع ميزان التشغيلة الأساسي، وتظهر الدفعات الإضافية في التتبع. بيانات التشغيل القديمة لا تحتوي روابط صرف يمكن استنتاجها تلقائيًا.

## إعادة التشغيل

```powershell
dotnet build 'back-end/ERP-Backend/ERP Api.csproj'
npm ci --prefix 'front-end/erp-frontend'
npm test --prefix 'front-end/erp-frontend'
npm ci --prefix 'front Deploy/erp-frontend'
npm test --prefix 'front Deploy/erp-frontend'
npm ci --prefix tests-audit
SqlLocalDB create ERP_Audit
SqlLocalDB start ERP_Audit
$env:ERP_AUDIT_SQL_SERVER = '(localdb)\ERP_Audit'
./tests-audit/Start-LocalAudit.ps1
```

لإضافة تمرين المتصفح، اضبط `ERP_AUDIT_BROWSER=1` ووفّر Chromium لـ Playwright أو عرّف `ERP_CHROMIUM_PATH` لمسار Chromium المحلي. السكربت بيجهز بيانات مؤقتة. فحص Git غير متاح لأن مجلد العمل خارج Git.
