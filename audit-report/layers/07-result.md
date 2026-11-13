---
layer: 7
name: Defensive Security
status: Partial
mode: executed
reason: فحص NuGet وnpm ومراجعة الكود. راجع SECURITY.md للضوابط والنتائج.
tests_run: 3
tests_passed: 3
tests_failed: 0
critical: 0
high: 0
medium: 1
low: 0
info: 0
---

فحص NuGet وnpm ومراجعة الكود. راجع SECURITY.md للضوابط والنتائج.

### L07-001 — تنبيهات Moderate في أدوات lint

- المكان: package-lock.json للواجهتين.
- التحقق: npm audit --json، والنتيجة في ../logs/npm-audit.json.
- المتوقع: مسار بناء باعتمادات تطوير مصححة.
- الفعلي: ستة تنبيهات في سلسلة TypeScript ESLint وminimatch. الفحص المنفصل لحزم التشغيل أعاد صفر تنبيهات.
- الأثر: زيادة سطح المخاطر عند تشغيل أدوات التطوير في CI.
- الإصلاح: ترقية مجموعة أدوات ESLint المتوافقة وإعادة فحص lint وnpm audit. الجهد M.
