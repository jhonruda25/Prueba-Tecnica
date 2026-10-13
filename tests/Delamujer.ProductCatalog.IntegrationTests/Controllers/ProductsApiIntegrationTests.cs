using System.Net;
using System.Net.Http.Json;
using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Delamujer.ProductCatalog.IntegrationTests.Controllers;

public class ProductsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_WithValidData_Returns201CreatedAndLocationHeader()
    {
        // Arrange
        var request = new CreateProductDto(
            "Crédito Capital de Trabajo",
            "Crédito para compra de insumos",
            1200000m,
            20);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Name.Should().Be(request.Name);
        body.Price.Should().Be(request.Price);
        body.Stock.Should().Be(request.InitialStock);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_Returns400BadRequest()
    {
        // Arrange: Price <= 0 and Empty Name
        var request = new CreateProductDto(
            "",
            "Descripción",
            0m,
            -5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var rawContent = await response.Content.ReadAsStringAsync();
        rawContent.Should().Contain("Name");
        rawContent.Should().Contain("Price");
        rawContent.Should().Contain("InitialStock");
    }

    [Fact]
    public async Task GetProductById_WhenExists_Returns200OK()
    {
        // Arrange: Create first
        var createDto = new CreateProductDto("Seguro Médico Mujer", "Cobertura de hospitalización", 35000m, 100);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/products/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task GetProductById_WhenNotFound_Returns404NotFoundWithProblemDetails()
    {
        // Arrange
        var randomId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{randomId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task GetPagedProducts_Returns200OKWithPaginationMetadata()
    {
        // Act
        var response = await _client.GetAsync("/api/products?pageNumber=1&pageSize=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var paged = await response.Content.ReadFromJsonAsync<PagedResultDto<ProductResponseDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().NotBeEmpty();
        paged.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(3);
        paged.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateProduct_Returns200OKWithUpdatedValues()
    {
        // Arrange: Create first
        var createDto = new CreateProductDto("Producto Antiguo", "Desc", 500m, 10);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();

        var updateDto = new UpdateProductDto("Producto Renovado", "Nueva Desc", 999m);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/products/{created!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Producto Renovado");
        updated.Price.Should().Be(999m);
        updated.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AdjustStock_WithPositiveDelta_IncreasesStock()
    {
        // Arrange: Create with stock 10
        var createDto = new CreateProductDto("Microcrédito Flex", "Desc", 100m, 10);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();

        var adjustDto = new AdjustStockDto(15);

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/products/{created!.Id}/stock", adjustDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        updated!.Stock.Should().Be(25);
    }

    [Fact]
    public async Task AdjustStock_WhenDeductionWouldResultInNegative_Returns400BadRequest()
    {
        // Arrange: Create with stock 5
        var createDto = new CreateProductDto("Microcrédito Express", "Desc", 100m, 5);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();

        var adjustDto = new AdjustStockDto(-6); // Exceeds 5

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/products/{created!.Id}/stock", adjustDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Insufficient Stock for Adjustment");
    }

    [Fact]
    public async Task DeleteProduct_Returns204NoContent()
    {
        // Arrange
        var createDto = new CreateProductDto("Producto Para Eliminar", "Desc", 100m, 10);
        var createRes = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<ProductResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify soft delete
        var getRes = await _client.GetAsync($"/api/products/{created.Id}");
        var fetched = await getRes.Content.ReadFromJsonAsync<ProductResponseDto>();
        fetched!.IsActive.Should().BeFalse();
    }
}
