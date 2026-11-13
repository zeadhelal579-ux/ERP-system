# ERP System | Vercel Demo

نسخة نشر واجهة مصنع التعبئة. تستخدم نفس مسارات التشغيل في الواجهة الأساسية، وملف `vercel.json` يضبط بناء Vite وروابط React عند فتحها مباشرة.

## إعداد Vercel

1. اربط الريبو وحدد `front-Deploy` كفرع الإنتاج.
2. خلي Root Directory على جذر الفرع. اختار Vite واضبط Build Command على `npm run build` وOutput Directory على `dist`.
3. للديمو العام، سيب `VITE_API_BASE_URL` بدون قيمة. اختار حساب عرض حسب الدور، وسجّل الخروج لتجربة دور تاني لعرض 36 دفعة خامة و54 تشغيلة و24 بالة. ابدأ بـ `CAPS-DEMO` في Lot recall. حجز الجودة محاكاة محلية بتتصفر عند تحديث الصفحة، وباقي البيانات للعرض.
4. للربط بباك حقيقي، اضبط `VITE_API_BASE_URL` على عنوان HTTPS شاملًا `/api/v1`، وأضف أصل Vercel إلى `AllowedOrigins`.
5. بعد النشر، جرّب تسجيل الدخول وفتح `/recall` و`/pallets` بأدوار مناسبة، وراجع استدعاء الدفعات والحجز والشحن.

## تحقق محلي

استخدم Node.js 24 مثل إعداد الفرع على Vercel.

```powershell
npm ci
npm run build
npm run preview
npm test
npm run lint
```

عند تجربة باك حقيقي محليًا، اضبط عنوانه واسمح بأصل Vite preview في CORS. [نتائج التحقق المحلي](https://github.com/zeadhelal579-ux/ERP-system/blob/main/audit-report/REPORT.md).
