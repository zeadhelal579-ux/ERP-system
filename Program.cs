using ERP_system.BackgroundJobs;
using ERP_system.Common;
using ERP_system.Data;
using ERP_system.Data.Seed;
using ERP_system.DTOs.Auth;
using ERP_system.DTOs.Lab;
using ERP_system.DTOs.Manager;
using ERP_system.DTOs.Production;
using ERP_system.DTOs.Scrap;
using ERP_system.DTOs.Shipping;
using ERP_system.DTOs.Store;
using ERP_system.Filters;
using ERP_system.Middlewares;
using ERP_system.Repositories;
using ERP_system.Services;
using ERP_system.Services.Interfaces;
using ERP_system.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Finding #12 + القسم 4.7: Serilog للـ Structured Logging التشغيلي — منفصل تمامًا
// عن AuditLog (اللي فضل زي ما هو: سجل أعمال/Compliance لا يُحذف منه شيء أبدًا).
// ============================================================================
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// ============================================================================
// Finding #1 (🔴 حرجة): مفتاح توقيع الـ JWT كان مكتوب صراحة كنص واضح في appsettings.json.
// دلوقتي بيتقرأ من الإعدادات (اللي المفروض تيجي من متغير بيئة Jwt__Key في الإنتاج،
// أو dotnet user-secrets في التطوير المحلي) — ومفيش أي قيمة افتراضية مكتوبة في الكود.
// الفحص ده Fail-Fast: التطبيق بيرفض يشتغل أصلاً لو المفتاح مش متعرّف، بدل ما يشتغل
// بصمت بإعداد غير آمن. راجع README.md لخطوات الإعداد بالتفصيل.
// ============================================================================
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "مفتاح توقيع الـ JWT (Jwt:Key) غير مُعرَّف. " +
        "استخدم 'dotnet user-secrets set \"Jwt:Key\" \"<قيمة عشوائية طويلة>\"' في التطوير المحلي، " +
        "أو عرّف متغير بيئة Jwt__Key في الإنتاج. راجع README.md للتفاصيل الكاملة.");
}

// ==== Database ====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ==== Repositories ====
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ==== Services ====
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IProductionService, ProductionService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IManagerService, ManagerService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IScrapService, ScrapService>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<BottlingService>();

// ============================================================================
// القسم 4.6: FluentValidation — تسجيل يدوي صريح لكل Validator بدل الاعتماد على
// AssemblyScanning (بيحتاج أحيانًا حزمة DependencyInjectionExtensions منفصلة حسب
// الإصدار)، عشان نضمن التوافق مع نسخة FluentValidation الأساسية فقط.
// ============================================================================
builder.Services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<AddRawMaterialDto>, AddRawMaterialValidator>();
builder.Services.AddScoped<IValidator<IssueMaterialDto>, IssueMaterialValidator>();
builder.Services.AddScoped<IValidator<RecordMaterialWasteDto>, RecordMaterialWasteValidator>();
builder.Services.AddScoped<IValidator<StartBatchDto>, StartBatchValidator>();
builder.Services.AddScoped<IValidator<FinishBatchDto>, FinishBatchValidator>();
builder.Services.AddScoped<IValidator<SubmitLabResultDto>, SubmitLabResultValidator>();
builder.Services.AddScoped<IValidator<ManagerDecisionDto>, ManagerDecisionValidator>();
builder.Services.AddScoped<IValidator<ScrapDecisionDto>, ScrapDecisionValidator>();
builder.Services.AddScoped<IValidator<CreateShipmentDto>, CreateShipmentValidator>();

// ============================================================================
// القسم 4.8: أول استخدام حقيقي لمجلد BackgroundJobs (كان فارغًا تمامًا قبل كده).
// ============================================================================
builder.Services.AddHostedService<ExpiryAlertBackgroundService>();

// ==== JWT Authentication ====
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// ============================================================================
// Finding #2 (🔴 حرجة): مفيش إعداد CORS إطلاقًا — أي طلب من فرونت React هيتم رفضه
// من المتصفح مباشرة. الأصول المسموحة بتتحدد من الإعدادات (AllowedOrigins) بدل ما
// تتكتب صريحة في الكود، عشان تختلف بسهولة بين بيئة التطوير والإنتاج.
// ============================================================================
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        // لو مفيش Origins معرّفة، السياسة مبترفضش تلقائيًا كل حاجة (مقصود)، لكن
        // من غير WithOrigins/AllowAnyMethod هتفضل سياسة فاضية عمليًا = رفض ضمني.
    });
});

// ============================================================================
// القسم 6 (End-to-End Security Checklist): Rate Limiting على تسجيل الدخول، عبر
// الـ Middleware المدمج في .NET (بدون أي حزمة NuGet خارجية زي AspNetCoreRateLimit).
// ============================================================================
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // ==========================================================================
    // اكتشاف أثناء مراجعة التوافق بين الفرونت والباك (لوب 10 - فحص شكل الاستجابة):
    // من غير OnRejected، رفض الـ Rate Limiter الافتراضي بيرجّع 429 بجسم فاضي تمامًا،
    // مخالفًا لوعد القسم 4.3 إن كل استجابة من الـ API (نجاح أو فشل) لازم تكون بنفس شكل
    // ApiResponse<T>. عمليًا: apiClient.ts في الفرونت كان هيفشل في قراءة envelope.message
    // من جسم فاضي، فيظهر للمستخدم رسالة "تعذّر الاتصال بالخادم" العامة بدل رسالة واضحة
    // عن سبب الرفض الحقيقي (محاولات دخول كتير) بعد 5 محاولات فاشلة على /auth/login.
    // ==========================================================================
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object?>.Fail(
            "TOO_MANY_REQUESTS",
            "محاولات كتير في وقت قصير، من فضلك انتظر دقيقة وحاول تاني"
        );

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.HttpContext.Response.WriteAsync(json, token);
    };
});

// ============================================================================
// القسم 4.6: تسجيل ValidationFilter كـ Global Filter على كل الـ Controllers.
// ============================================================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ERP Api",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "اكتب التوكن بالشكل ده: Bearer {التوكن}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// لازم يكون أول Middleware عشان يلف حوالين كل حاجة تانية (اسم الكلاس اتغيّر لـ
// ExceptionHandlerMiddleware مع إضافة معالجة الاستثناءات المخصصة - القسم 4.4)
app.UseAppExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Finding #3 (🟠 عالية): كانت معطّلة (Commented Out) بالكامل
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// القسم 4.7: Health Check بسيط - ضروري لأي بيئة نشر حقيقية (Load Balancer/Container)
app.MapGet("/health", async (AppDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "healthy", database = "connected" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

// القسم 4.2 (Data/Seed): تنفيذ الـ Migrations + بيانات الأدوار والـ SuperAdmin الأولية
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DataSeeder.SeedAsync(context, app.Configuration, seedLogger);
}

app.Run();
