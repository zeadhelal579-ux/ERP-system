using ERP_system.DTOs.Scrap;
using FluentValidation;

namespace ERP_system.Validators
{
    public class ScrapDecisionValidator : AbstractValidator<ScrapDecisionDto>
    {
        private static readonly string[] ValidDecisions = { "Destroy", "RequestRework" };

        public ScrapDecisionValidator()
        {
            RuleFor(x => x.ScrapEntryId).GreaterThan(0);
            RuleFor(x => x.Decision).NotEmpty()
                .Must(d => ValidDecisions.Contains(d))
                .WithMessage($"القرار لازم يكون واحد من: {string.Join(", ", ValidDecisions)}");
        }
    }
}
