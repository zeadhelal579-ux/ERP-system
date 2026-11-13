# نطاق التحقق

- Backend: ASP.NET Core 10 وEF Core وSQL Server، JWT وFluentValidation وSwagger للتطوير. مهمة خلفية تسجل تحذيرات الصلاحية. مفيش دفع أو تكاملات ERP أو تحكم بالماكينات في النسخة الحالية.
- Frontend: React 18 وVite وTanStack Query وReact Router. نسخة التطوير داخل `front-end/erp-frontend` ونسخة النشر داخل `front Deploy/erp-frontend`.
- الأدوار: SuperAdmin، Storekeeper، ProductionManager، LabTech، QualityManager، GeneralManager. جرد مسارات الـ API في [route-inventory.txt](logs/route-inventory.txt).
- الشاشات: Login، Inventory، Production، Lab، Approvals، Quarantine، Shipping، Overview، Traceability، Pallets & dispatch، Lot recall. الصلاحيات من `src/app/App.tsx` في النسختين.
- فحص Git قبل وبعد غير متاح لأن المجلد مفيهوش Git repository.
