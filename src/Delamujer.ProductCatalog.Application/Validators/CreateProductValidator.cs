using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Domain.Entities;
using FluentValidation;

namespace Delamujer.ProductCatalog.Application.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(Product.MaxNameLength)
            .WithMessage($"Product name cannot exceed {Product.MaxNameLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(Product.MaxDescriptionLength)
            .WithMessage($"Product description cannot exceed {Product.MaxDescriptionLength} characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Product price must be strictly greater than zero.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Initial stock cannot be negative.");
    }
}
