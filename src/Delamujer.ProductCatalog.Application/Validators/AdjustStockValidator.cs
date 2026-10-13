using Delamujer.ProductCatalog.Application.DTOs;
using FluentValidation;

namespace Delamujer.ProductCatalog.Application.Validators;

public class AdjustStockValidator : AbstractValidator<AdjustStockDto>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithMessage("Stock quantity delta cannot be zero. Specify a positive amount to increase or negative to decrease.");
    }
}
