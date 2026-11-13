using ERP_system.DTOs.Auth;

namespace ERP_system.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>يطلق BusinessRuleException("INVALID_CREDENTIALS", ...) لو اسم المستخدم أو الباسورد غلط</summary>
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

        /// <summary>يطلق ConflictException لو اليوزرنيم مستخدم، أو NotFoundException لو الدور مش موجود</summary>
        Task RegisterAsync(RegisterRequestDto request);
    }
}
