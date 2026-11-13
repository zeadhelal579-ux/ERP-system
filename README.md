# ERP System | Backend

API لعمليات مصنع التعبئة، مبني بـ ASP.NET Core 10 وEF Core وSQL Server. يغطي استلام الخامات، الفحص والصرف، بدء وإنهاء التشغيلات، اعتماد الجودة، التعبئة والشحن والتتبع.

## التشغيل المحلي

المتطلبات هي .NET 10 SDK وSQL Server أو SQL Server LocalDB. من جذر الفرع، اضبط المتغيرات في PowerShell.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=ErpBeverageLocal;Integrated Security=true;TrustServerCertificate=true'
$env:Jwt__Key = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Seed__SuperAdminPassword = '<choose a strong password>'
dotnet restore
dotnet run
```

التطبيق يطبّق الـ migrations ويجهز الأدوار وحساب SuperAdmin عند أول تشغيل لو كلمة المرور متاحة. أضف منتجًا في كتالوج `Products` ببيانات الموقع قبل بدء أول تشغيلة. استخدم قاعدة منفصلة للتجربة. راجع إعدادات `AllowedOrigins` عند توصيل الواجهة، واحتفظ بالمفتاح وكلمة المرور في إعدادات سرية خارج الريبو.

- Swagger للتطوير على `https://localhost:7158/swagger`.
- عنوان API المحلي الافتراضي للواجهة هو `http://localhost:5182/api/v1`.
- فحص الاتصال بقاعدة البيانات متاح في `/health`.

## سلوك التشغيل

بدء تشغيلة جديدة يحتاج حركة صرف أساسية، مع إمكانية ربط حركات إضافية من دفعات مختلفة. أعد استخدام نفس معرّف الطلب عند إعادة إرسال نفس عملية الصرف أو بدء التشغيل أو الشحن. استخدم مسار البالات مع التشغيلات المرتبطة بحركات الصرف. التشغيلات القديمة تدعم مسار الشحنة الكاملة.

Migration `AddBottlingTraceability` تضيف الجداول والقيود اللازمة. الدفعات القديمة تحصل على رمز `LEGACY` داخلي. تاريخ الصرف غير المتاح يحتاج تسوية لو بياناته موجودة في نظام آخر. راجع تكرار أسماء المستخدمين وأرقام التشغيلات قبل الترقية على قاعدة قديمة.

نتائج التشغيل المحلي في [تقرير الـ Pilot](https://github.com/zeadhelal579-ux/ERP-system/blob/main/audit-report/REPORT.md).
