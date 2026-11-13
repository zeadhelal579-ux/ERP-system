using ERP_system.DTOs.Manager;
using FluentValidation;

namespace ERP_system.Validators
{
    public class ManagerDecisionValidator : AbstractValidator<ManagerDecisionDto>
    {
        public ManagerDecisionValidator()
        {
            RuleFor(x => x.BatchId).GreaterThan(0);
            RuleFor(x => x.Reason).NotEmpty().When(x => !x.Approve)
                .WithMessage("سبب الرفض مطلوب عند رفض التشغيلة");
        }
    }
}
