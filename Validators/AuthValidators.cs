using ERP_system.DTOs.Auth;
using FluentValidation;

namespace ERP_system.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("اسم المستخدم مطلوب");
            RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة");
        }
    }

    public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("الاسم الكامل مطلوب").MaximumLength(100);
            RuleFor(x => x.Username).NotEmpty().WithMessage("اسم المستخدم مطلوب").MaximumLength(50);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور لازم تكون 6 أحرف على الأقل");
            RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("الدور مطلوب");
        }
    }
}
