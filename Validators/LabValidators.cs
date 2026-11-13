using ERP_system.DTOs.Lab;
using FluentValidation;

namespace ERP_system.Validators
{
    public class SubmitLabResultValidator : AbstractValidator<SubmitLabResultDto>
    {
        private static readonly string[] ValidTypes = { "IncomingMaterial", "FinishedBatch" };

        public SubmitLabResultValidator()
        {
            RuleFor(x => x.InspectionType).NotEmpty()
                .Must(t => ValidTypes.Contains(t))
                .WithMessage($"نوع الفحص لازم يكون واحد من: {string.Join(", ", ValidTypes)}");

            RuleFor(x => x.ResultDetails).NotEmpty().WithMessage("تفاصيل نتيجة الفحص مطلوبة");

            RuleFor(x => x)
                .Must(x => x.BatchId.HasValue ^ x.RawMaterialId.HasValue)
                .WithMessage("لازم تحديد مرجع واحد بس: إما خامة أو تشغيلة، مش الاتنين ومش ولا واحدة")
                .WithName("Reference");
        }
    }
}
