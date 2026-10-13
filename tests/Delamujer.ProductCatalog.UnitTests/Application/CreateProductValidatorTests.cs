using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Delamujer.ProductCatalog.UnitTests.Application;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void Validate_WithValidDto_ShouldBeValid()
    {
        var dto = new CreateProductDto("Microcrédito delamujer", "Línea de crédito productiva", 120000m, 10);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyName_ShouldHaveValidationError(string? invalidName)
    {
        var dto = new CreateProductDto(invalidName!, "Desc", 100m, 5);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductDto.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithZeroOrNegativePrice_ShouldHaveValidationError(decimal invalidPrice)
    {
        var dto = new CreateProductDto("Nombre Válido", "Desc", invalidPrice, 5);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductDto.Price));
    }

    [Fact]
    public void Validate_WithNegativeStock_ShouldHaveValidationError()
    {
        var dto = new CreateProductDto("Nombre Válido", "Desc", 100m, -1);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductDto.InitialStock));
    }
}
