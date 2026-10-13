using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.Application.Services;
using Delamujer.ProductCatalog.Application.Validators;
using Delamujer.ProductCatalog.Domain.Entities;
using Delamujer.ProductCatalog.Domain.Exceptions;
using Delamujer.ProductCatalog.Domain.Repositories;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Delamujer.ProductCatalog.UnitTests.Application;

public class ProductServiceTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CreateProductValidator _createValidator = new();
    private readonly UpdateProductValidator _updateValidator = new();
    private readonly AdjustStockValidator _adjustStockValidator = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repository,
            _unitOfWork,
            _createValidator,
            _updateValidator,
            _adjustStockValidator);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ShouldPersistAndReturnDto()
    {
        // Arrange
        var dto = new CreateProductDto("Seguro Vida Mujer", "Póliza integral", 25000m, 50);

        // Act
        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(dto.Name);
        result.Price.Should().Be(dto.Price);
        result.Stock.Should().Be(dto.InitialStock);

        await _repository.Received(1).AddAsync(Arg.Is<Product>(p => p.Name == dto.Name), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDto_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateProductDto("", "Desc", -10m, -5);

        // Act
        var act = () => _sut.CreateAsync(dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldReturnMappedDto()
    {
        // Arrange
        var product = Product.Create("Ahorro delamujer", "Cuenta de ahorro", 1000m, 100);
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        var result = await _sut.GetByIdAsync(product.Id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(product.Id);
        result.Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ShouldThrowProductNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var act = () => _sut.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>()
                 .Where(e => e.ProductId == nonExistentId);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenAtomicAdjustmentSucceeds_ShouldReturnUpdatedDto()
    {
        // Arrange
        var product = Product.Create("Producto A", "Desc", 1000m, 20);
        product.AdjustStock(5); // Stock now 25

        _repository.AdjustStockAtomicAsync(product.Id, 5, Arg.Any<CancellationToken>())
                   .Returns(product);

        var dto = new AdjustStockDto(5);

        // Act
        var result = await _sut.AdjustStockAsync(product.Id, dto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Stock.Should().Be(25);
    }

    [Fact]
    public async Task AdjustStockAsync_WhenProductNotFound_ShouldThrowProductNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.AdjustStockAtomicAsync(id, -3, Arg.Any<CancellationToken>())
                   .Returns((Product?)null);

        var dto = new AdjustStockDto(-3);

        // Act
        var act = () => _sut.AdjustStockAsync(id, dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>()
                 .Where(e => e.ProductId == id);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_ShouldDeactivateAndSave()
    {
        // Arrange
        var product = Product.Create("Producto B", "Desc", 1000m, 10);
        _repository.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        await _sut.DeleteAsync(product.Id, CancellationToken.None);

        // Assert
        product.IsActive.Should().BeFalse();
        _repository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
