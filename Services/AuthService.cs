using ERP_system.DTOs.Auth;
using ERP_system.Exceptions;
using ERP_system.Models;
using ERP_system.Repositories;
using ERP_system.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ERP_system.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<object> _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<object>();
            _logger = logger;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Username == request.Username && u.IsActive);
            var user = users.FirstOrDefault();

            if (user == null)
            {
                _logger.LogWarning("محاولة دخول فاشلة: يوزرنيم غير موجود أو غير مفعّل - {Username}", request.Username);
                throw new BusinessRuleException("INVALID_CREDENTIALS", "اسم المستخدم أو كلمة المرور غير صحيحة");
            }

            var verifyResult = _passwordHasher.VerifyHashedPassword(new object(), user.PasswordHash, request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("محاولة دخول فاشلة: باسورد غلط للمستخدم {Username}", request.Username);
                throw new BusinessRuleException("INVALID_CREDENTIALS", "اسم المستخدم أو كلمة المرور غير صحيحة");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            var roleName = role?.RoleName ?? "Unknown";

            var token = GenerateJwtToken(user.Id, user.Username, roleName);
            // Finding #12: توحيد كل توقيتات النظام على UTC
            var expiresAt = DateTime.UtcNow.AddHours(8);

            _logger.LogInformation("دخول ناجح للمستخدم {Username} بدور {Role}", user.Username, roleName);

            return new LoginResponseDto
            {
                Token = token,
                FullName = user.FullName,
                Role = roleName,
                ExpiresAt = expiresAt
            };
        }

        public async Task RegisterAsync(RegisterRequestDto request)
        {
            var existingUsers = await _unitOfWork.Users.FindAsync(u => u.Username == request.Username);
            if (existingUsers.Any())
                throw new ConflictException("اسم المستخدم مستخدم بالفعل", "USERNAME_TAKEN");

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId);
            if (role == null)
                throw NotFoundException.For("الدور المطلوب", "ROLE_NOT_FOUND");

            var hashedPassword = _passwordHasher.HashPassword(new object(), request.Password);

            var newUser = new User
            {
                FullName = request.FullName,
                Username = request.Username,
                PasswordHash = hashedPassword,
                RoleId = request.RoleId,
                IsActive = true
            };

            await _unitOfWork.Users.AddAsync(newUser);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // خط دفاع ثانٍ: لو حصل Race Condition حقيقي بين لحظة التحقق فوق ولحظة
                // الحفظ (طلبين تسجيل بنفس اليوزرنيم في نفس اللحظة بالظبط)، الفهرس الـ
                // Unique على مستوى القاعدة (Finding #7) هيرفض الحفظ الثاني، وهنا بنحوّل
                // الاستثناء الخام لرسالة عربية واضحة بدل ما يوصل كـ 500 عام.
                _logger.LogWarning(ex, "تعارض عند تسجيل يوزر جديد - {Username}", request.Username);
                throw new ConflictException("اسم المستخدم مستخدم بالفعل", "USERNAME_TAKEN");
            }

            _logger.LogInformation("تم تسجيل مستخدم جديد {Username} بدور {RoleName}", newUser.Username, role.RoleName);
        }

        private string GenerateJwtToken(int userId, string username, string role)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                // Finding #12: توحيد كل توقيتات النظام على UTC
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
