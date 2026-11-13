# ERP System | Frontend

واجهة تشغيل مصنع تعبئة مبنية بـ React وTypeScript وVite. تشمل المخزون والإنتاج والمعمل وقرارات الجودة والشحن والتتبع. صلاحيات العمليات النهائية يطبقها الـ API.

## تشغيل التطوير

استخدم Node.js 22.12 أو أحدث ضمن سلسلة 22، أو Node.js 24.

```powershell
npm ci
Copy-Item .env.example .env
npm run dev
```

اضبط `VITE_API_BASE_URL` على عنوان الباك شاملًا `/api/v1`. الافتراضي المحلي `http://localhost:5182/api/v1`. شغّل الباك أولًا واسمح بأصل واجهة التطوير في CORS، عادة `http://localhost:5173`.

```powershell
npm run build
npm run lint
npm test
```

سيناريو الديمو يبدأ بدفعة أغطية تُصرف لتشغيلتين. بعد اعتماد الجودة وتعبئة البالات، يُشحن جزء من المنتج. شاشة **Lot recall** تعرض التشغيلات والبالات والعميل وتتيح لمدير الجودة حجز الكمية التي ما زالت في المصنع.

[تقرير الاختبار](https://github.com/zeadhelal579-ux/ERP-system/blob/main/audit-report/REPORT.md)
