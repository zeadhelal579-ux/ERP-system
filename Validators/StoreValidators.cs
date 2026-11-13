using ERP_system.DTOs.Store;
using FluentValidation;

namespace ERP_system.Validators
{
    public class AddRawMaterialValidator : AbstractValidator<AddRawMaterialDto>
    {
        private static readonly string[] ValidStates = { "Liquid", "Solid", "Frozen", "Powder" };

        public AddRawMaterialValidator()
        {
            RuleFor(x => x.MaterialName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Quantity).GreaterThan(0).PrecisionScale(18, 2, true).WithMessage("الكمية لازم تكون أكبر من صفر");
            RuleFor(x => x.PhysicalState).NotEmpty()
                .Must(s => ValidStates.Contains(s))
                .WithMessage($"الحالة الفيزيائية لازم تكون واحدة من: {string.Join(", ", ValidStates)}");
            RuleFor(x => x.ExpiryDate).GreaterThan(_ => DateTime.UtcNow.Date)
                .WithMessage("تاريخ الصلاحية لازم يكون في المستقبل");
        }
    }

    public class IssueMaterialValidator : AbstractValidator<IssueMaterialDto>
    {
        public IssueMaterialValidator()
        {
            RuleFor(x => x.RawMaterialId).GreaterThan(0);
            RuleFor(x => x.RequestId).NotEqual(Guid.Empty);
            RuleFor(x => x.QuantityToIssue).GreaterThan(0).PrecisionScale(18, 2, true).WithMessage("كمية الصرف لازم تكون أكبر من صفر");
        }
    }

    public class RecordMaterialWasteValidator : AbstractValidator<RecordMaterialWasteDto>
    {
        public RecordMaterialWasteValidator()
        {
            RuleFor(x => x.RawMaterialId).GreaterThan(0);
            RuleFor(x => x.WastedQuantity).GreaterThan(0).PrecisionScale(18, 2, true).WithMessage("كمية الهالك لازم تكون أكبر من صفر");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("سبب الهالك مطلوب");
        }
    }
}
