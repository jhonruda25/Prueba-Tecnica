using System.Net;
using System.Net.Http.Json;
using Delamujer.ProductCatalog.Application.DTOs;
using Delamujer.ProductCatalog.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace Delamujer.ProductCatalog.IntegrationTests.Concurrency;

public class StockConcurrencyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StockConcurrencyIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AdjustStock_UnderConcurrentDeductions_MaintainsExactStockWithoutRaceConditions()
    {
        // Arrange: Initial stock = 100
        const int initialStock = 100;
        const int concurrentRequests = 20;
        const int unitsPerRequest = 5; // 20 * 5 = 100

        var createDto = new CreateProductDto(
            "Producto Concurrente Seguro",
            "Prueba de estrés transaccional",
            50000m,
            initialStock);

        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        var productId = created!.Id;

        // Act: Launch 20 concurrent threads simultaneously
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            var adjustDto = new AdjustStockDto(-unitsPerRequest);
            return await _client.PatchAsJsonAsync($"/api/products/{productId}/stock", adjustDto);
        });

        var responses = await Task.WhenAll(tasks);

        // Assert: All 20 requests succeeded
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

        // Verify final stock is exactly 0 (no lost updates)
        var finalResponse = await _client.GetAsync($"/api/products/{productId}");
        var finalProduct = await finalResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        finalProduct!.Stock.Should().Be(0, "all 20 concurrent decrements of 5 units from 100 should leave balance at 0");
    }

    [Fact]
    public async Task AdjustStock_WhenConcurrentDeductionsExceedInventory_RejectsExcessAndNeverAllowsNegativeStock()
    {
        // Arrange: Initial stock = 10
        const int initialStock = 10;
        const int concurrentRequests = 10;
        const int unitsPerRequest = 2; // Total requested = 20, but only 10 available (5 should pass, 5 should fail)

        var createDto = new CreateProductDto(
            "Inventario Limitado",
            "Prueba de sobregiro concurrente",
            10000m,
            initialStock);

        var createResponse = await _client.PostAsJsonAsync("/api/products", createDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponseDto>();
        var productId = created!.Id;

        // Act: Fire 10 concurrent requests at the exact same time
        var tasks = Enumerable.Range(0, concurrentRequests).Select(async _ =>
        {
            var adjustDto = new AdjustStockDto(-unitsPerRequest);
            return await _client.PatchAsJsonAsync($"/api/products/{productId}/stock", adjustDto);
        });

        var responses = await Task.WhenAll(tasks);

        // Assert: Exactly 5 should succeed (200 OK) and exactly 5 should fail (400 Bad Request)
        int successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        int failureCount = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        successCount.Should().Be(5, "only 5 requests of 2 units each can be fulfilled from 10 available units");
        failureCount.Should().Be(5, "the remaining 5 requests must be rejected with 400 Bad Request");

        // Verify final stock never became negative
        var finalResponse = await _client.GetAsync($"/api/products/{productId}");
        var finalProduct = await finalResponse.Content.ReadFromJsonAsync<ProductResponseDto>();

        finalProduct!.Stock.Should().Be(0, "inventory must be exactly 0, never negative under concurrent race conditions");
    }
}
