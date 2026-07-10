using System.Net;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Modules;

/// <summary>
/// Integration tests for health check endpoints.
/// Verifies all three health endpoints follow Kubernetes probe patterns.
/// </summary>
public class HealthModuleTests : IClassFixture<Integration.TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthModuleTests(Integration.TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().ShouldBe("Healthy");
        healthReport.TryGetProperty("totalDuration", out _).ShouldBeTrue();
        healthReport.TryGetProperty("entries", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GetHealth_IncludesDatabaseCheck()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        var entries = healthReport.GetProperty("entries");
        
        // Should include database health check
        entries.TryGetProperty("appdbcontext", out var dbCheck).ShouldBeTrue();
        dbCheck.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetHealthReady_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().ShouldBe("Healthy");
    }

    [Fact]
    public async Task GetHealthReady_OnlyIncludesTaggedChecks()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        var entries = healthReport.GetProperty("entries");
        
        // Should only include checks tagged with "ready"
        entries.TryGetProperty("appdbcontext", out var dbCheck).ShouldBeTrue();
        var tags = dbCheck.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        tags.ShouldContain("ready");
    }

    [Fact]
    public async Task GetHealthLive_AlwaysReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);
        healthReport.GetProperty("status").GetString().ShouldBe("Healthy");
        healthReport.GetProperty("message").GetString().ShouldBe("Process is alive");
    }

    [Fact]
    public async Task AllHealthEndpoints_AllowAnonymousAccess()
    {
        // Arrange - No authentication headers

        // Act & Assert - Should not require authentication
        var healthResponse = await _client.GetAsync("/health");
        healthResponse.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);

        var readyResponse = await _client.GetAsync("/health/ready");
        readyResponse.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);

        var liveResponse = await _client.GetAsync("/health/live");
        liveResponse.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthEndpoints_NotUnderV1Path()
    {
        // Act - Health checks should be at root, not under /v1
        var rootHealth = await _client.GetAsync("/health");
        var v1Health = await _client.GetAsync("/v1/health");

        // Assert
        rootHealth.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1Health.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
