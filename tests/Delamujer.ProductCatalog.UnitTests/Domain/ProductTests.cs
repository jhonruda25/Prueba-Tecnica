using Delamujer.ProductCatalog.Domain.Entities;
using Delamujer.ProductCatalog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Delamujer.ProductCatalog.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInstantiateProduct()
    {
        // Arrange
        const string name = "Microcrédito Semilla";
        const string description = "Financiamiento para microempresarias de bajos recursos";
        const decimal price = 150000m;
        const int stock = 25;

        // Act
        var product = Product.Create(name, description, price, stock);

        // Assert
        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be(name);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.Stock.Should().Be(stock);
        product.IsActive.Should().BeTrue();
        product.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        product.UpdatedAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowDomainValidationException(string? invalidName)
    {
        // Act
        var act = () => Product.Create(invalidName!, "Valid description", 1000m, 10);

        // Assert
        act.Should().Throw<DomainValidationException>()
           .WithMessage("*Product name is required*");
    }

    [Fact]
    public void Create_WithNameExceedingMaxLength_ShouldThrowDomainValidationException()
    {
        // Arrange
        var tooLongName = new string('A', Product.MaxNameLength + 1);

        // Act
        var act = () => Product.Create(tooLongName, "Description", 1000m, 10);

        // Assert
        act.Should().Throw<DomainValidationException>()
           .WithMessage($"*cannot exceed {Product.MaxNameLength} characters*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithZeroOrNegativePrice_ShouldThrowDomainValidationException(decimal invalidPrice)
    {
        // Act
        var act = () => Product.Create("Producto Válido", "Desc", invalidPrice, 5);

        // Assert
        act.Should().Throw<DomainValidationException>()
           .WithMessage("*price must be strictly greater than zero*");
    }

    [Fact]
    public void Create_WithNegativeInitialStock_ShouldThrowDomainValidationException()
    {
        // Act
        var act = () => Product.Create("Producto Válido", "Desc", 1000m, -1);

        // Assert
        act.Should().Throw<DomainValidationException>()
           .WithMessage("*Initial stock cannot be negative*");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdatePropertiesAndTimestamp()
    {
        // Arrange
        var product = Product.Create("Original", "Original Desc", 500m, 10);

        // Act
        product.UpdateDetails("Nuevo Nombre", "Nueva Desc", 750m);

        // Assert
        product.Name.Should().Be("Nuevo Nombre");
        product.Description.Should().Be("Nueva Desc");
        product.Price.Should().Be(750m);
        product.UpdatedAtUtc.Should().NotBeNull();
        product.UpdatedAtUtc!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AdjustStock_WithPositiveDelta_ShouldIncreaseStock()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 100m, 10);

        // Act
        product.AdjustStock(5);

        // Assert
        product.Stock.Should().Be(15);
        product.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AdjustStock_WithValidNegativeDelta_ShouldDecreaseStock()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 100m, 10);

        // Act
        product.AdjustStock(-7);

        // Assert
        product.Stock.Should().Be(3);
        product.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AdjustStock_WhenDeductionExceedsStock_ShouldThrowInsufficientStockException()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 100m, 5);

        // Act
        var act = () => product.AdjustStock(-6);

        // Assert
        act.Should().Throw<InsufficientStockException>()
           .Where(e => e.CurrentStock == 5 && e.RequestedDelta == -6 && e.ProductId == product.Id);
    }

    [Fact]
    public void AdjustStock_WithZeroDelta_ShouldThrowDomainValidationException()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 100m, 5);

        // Act
        var act = () => product.AdjustStock(0);

        // Assert
        act.Should().Throw<DomainValidationException>()
           .WithMessage("*delta cannot be zero*");
    }

    [Fact]
    public void DeactivateAndActivate_ShouldToggleStatusAndTouchUpdatedAt()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 100m, 5);
        product.IsActive.Should().BeTrue();

        // Act: Deactivate
        product.Deactivate();
        product.IsActive.Should().BeFalse();
        product.UpdatedAtUtc.Should().NotBeNull();

        // Act: Activate
        product.Activate();
        product.IsActive.Should().BeTrue();
    }
}
