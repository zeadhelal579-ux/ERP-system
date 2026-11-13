using ERP_system.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERP_system.Data.Seed
{
    /// <summary>
    /// بيانات أولية (Seed Data): الأدوار الستة الفعلية في النظام + مستخدم SuperAdmin واحد
    /// عشان يقدر أول حد يدخل النظام أصلاً وينشئ باقي المستخدمين من خلاله.
    /// القسم 4.2 من التصميم المعماري: "Data/Seed/ (جديد: بيانات أولية - الأدوار + مستخدم SuperAdmin)".
    ///
    /// بيتنفّذ مرة واحدة عند إقلاع التطبيق (شوف Program.cs)، وهو Idempotent بالكامل:
    /// لو الأدوار أو الـ SuperAdmin موجودين بالفعل، مبيعملش حاجة تانية ومش هيكرر الإدخال.
    /// </summary>
    public static class DataSeeder
    {
        private static readonly string[] AllRoles =
        {
            "SuperAdmin",
            "Storekeeper",
            "ProductionManager",
            "LabTech",
            "QualityManager",
            "GeneralManager"
        };

        public static async Task SeedAsync(AppDbContext context, IConfiguration configuration, ILogger logger)
        {
            // تأكد إن قاعدة البيانات فيها آخر Migrations قبل ما نحاول نضيف أي حاجة
            await context.Database.MigrateAsync();

            foreach (var roleName in AllRoles)
            {
                var exists = await context.Roles.AnyAsync(r => r.RoleName == roleName);
                if (!exists)
                {
                    context.Roles.Add(new Role { RoleName = roleName });
                    logger.LogInformation("تمت إضافة دور جديد أثناء الـ Seeding: {RoleName}", roleName);
                }
            }

            await context.SaveChangesAsync();

            var anySuperAdmin = await context.Users.AnyAsync(u =>
                context.Roles.Any(r => r.Id == u.RoleId && r.RoleName == "SuperAdmin"));

            if (!anySuperAdmin)
            {
                var superAdminRole = await context.Roles.FirstAsync(r => r.RoleName == "SuperAdmin");

                // كلمة السر الأولية بتتاخد من الإعدادات (متغير بيئة أو user-secrets)، ومفيش قيمة
                // افتراضية مكتوبة صراحة في الكود — نفس مبدأ عدم تسريب الأسرار المطبّق على مفتاح الـ JWT.
                var initialPassword = configuration["Seed:SuperAdminPassword"];

                if (string.IsNullOrWhiteSpace(initialPassword))
                {
                    logger.LogWarning(
                        "لا يوجد مستخدم SuperAdmin ولا توجد قيمة Seed:SuperAdminPassword في الإعدادات — " +
                        "تم تخطي إنشاء مستخدم SuperAdmin. راجع README.md لمعرفة طريقة تحديدها.");
                    return;
                }

                var hasher = new PasswordHasher<object>();
                var superAdmin = new User
                {
                    FullName = "System Administrator",
                    Username = configuration["Seed:SuperAdminUsername"] ?? "superadmin",
                    PasswordHash = hasher.HashPassword(new object(), initialPassword),
                    RoleId = superAdminRole.Id,
                    IsActive = true
                };

                context.Users.Add(superAdmin);
                await context.SaveChangesAsync();

                logger.LogInformation(
                    "تم إنشاء مستخدم SuperAdmin أولي باسم '{Username}' — غيّر كلمة السر فورًا بعد أول دخول.",
                    superAdmin.Username);
            }
        }
    }
}
