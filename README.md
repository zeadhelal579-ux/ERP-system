# ERP System | Vercel Demo

نسخة نشر واجهة مصنع التعبئة. تستخدم نفس مسارات التشغيل في الواجهة الأساسية، وملف `vercel.json` يضبط بناء Vite وروابط React عند فتحها مباشرة.

## إعداد Vercel

1. اربط الريبو وحدد `front-Deploy` كفرع الإنتاج.
2. خلي Root Directory على جذر الفرع. اختار Vite واضبط Build Command على `npm run build` وOutput Directory على `dist`.
3. اضبط `VITE_API_BASE_URL` على عنوان الباك المنشور بـ HTTPS، شاملًا `/api/v1`.
4. أضف أصل واجهة Vercel بالضبط إلى `AllowedOrigins` في الباك.
5. بعد النشر، جرّب تسجيل الدخول وفتح `/recall` و`/pallets` بأدوار مناسبة، وراجع استدعاء الدفعات والحجز والشحن.

## تحقق محلي

استخدم Node.js 22.12 أو أحدث ضمن سلسلة 22، أو Node.js 24.

```powershell
npm ci
$env:VITE_API_BASE_URL = 'http://localhost:5182/api/v1'
npm run build
npm run preview
npm test
npm run lint
```

اسمح بأصل Vite preview في CORS لاختبار التكامل محليًا. [نتائج التحقق المحلي](https://github.com/zeadhelal579-ux/ERP-system/blob/main/audit-report/REPORT.md).
