using ERP_system.DTOs.Shipping;
using FluentValidation;

namespace ERP_system.Validators
{
    public class CreateShipmentValidator : AbstractValidator<CreateShipmentDto>
    {
        public CreateShipmentValidator()
        {
            RuleFor(x => x.BatchId).GreaterThan(0);
            RuleFor(x => x.ClientName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DriverName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TruckPlates).NotEmpty().MaximumLength(50);
        }
    }
}
