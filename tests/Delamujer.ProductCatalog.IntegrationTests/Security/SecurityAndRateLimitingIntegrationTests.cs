using System.Net;
using Delamujer.ProductCatalog.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace Delamujer.ProductCatalog.IntegrationTests.Security;

public class SecurityAndRateLimitingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityAndRateLimitingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_IncludesStrictSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.Headers.Should().Contain(h => h.Key == "X-Content-Type-Options" && h.Value.Contains("nosniff"));
        response.Headers.Should().Contain(h => h.Key == "X-Frame-Options" && h.Value.Contains("DENY"));
        response.Headers.Should().Contain(h => h.Key == "Referrer-Policy" && h.Value.Contains("strict-origin-when-cross-origin"));
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
