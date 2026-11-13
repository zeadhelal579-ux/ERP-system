# بيئة الاختبار

Windows، .NET SDK 10، Node 24، SQL Server LocalDB 17 وChromium. استخدم الاختبار instance محلية اسمها ERP_Audit وقواعد منفصلة بأسماء عشوائية. سكربت التشغيل يولد بيانات الدخول ومفتاح JWT أثناء الاختبار. اتجهز Frontend التطوير وVite preview لنسخة النشر، والاتنين اتصلوا بالـ API الحقيقي على loopback. فشل MSSQLLocalDB المشترك في إعادة التشغيل خلال الاختبار، والـ instance المنفصلة اشتغلت. راجع خطوات إعادة التشغيل في REPORT.md.
