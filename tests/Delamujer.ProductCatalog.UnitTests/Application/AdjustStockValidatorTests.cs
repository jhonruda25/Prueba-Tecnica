using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Application.Validators;
using FluentAssertions;
using Xunit;

namespace Delamujer.ProductCatalog.UnitTests.Application;

public class AdjustStockValidatorTests
{
    private readonly AdjustStockValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Validate_WithNonZeroDelta_ShouldBeValid(int delta)
    {
        var dto = new AdjustStockDto(delta);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroDelta_ShouldHaveValidationError()
    {
        var dto = new AdjustStockDto(0);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AdjustStockDto.QuantityDelta));
    }
}
