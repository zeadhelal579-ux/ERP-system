using ERP_system.DTOs.Production;
using FluentValidation;

namespace ERP_system.Validators
{
    public class StartBatchValidator : AbstractValidator<StartBatchDto>
    {
        public StartBatchValidator()
        {
            RuleFor(x => x.AdditionalIssueIds).NotNull();
            RuleFor(x => x.RequestId).NotEqual(Guid.Empty);
            RuleFor(x => x.ProductId).GreaterThan(0);
            RuleFor(x => x.RawMaterialId).GreaterThan(0);
            RuleFor(x => x.IssuedMaterialQty).GreaterThan(0).PrecisionScale(18, 2, true).WithMessage("الكمية المصروفة لازم تكون أكبر من صفر");
        }
    }

    public class FinishBatchValidator : AbstractValidator<FinishBatchDto>
    {
        public FinishBatchValidator()
        {
            RuleFor(x => x.BatchId).GreaterThan(0);
            RuleFor(x => x.ProducedQty).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).WithMessage("الكمية المنتَجة لا يمكن أن تكون سالبة");
            RuleFor(x => x.ReturnedMaterialQty).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true).WithMessage("الكمية المرتجعة لا يمكن أن تكون سالبة");
            RuleFor(x => x.ExpiryDate).GreaterThan(_ => DateTime.UtcNow.Date)
                .WithMessage("تاريخ الصلاحية لازم يكون في المستقبل");
        }
    }
}
