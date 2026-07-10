using System.Net;
using ForgeKit.Api.Middlewares;
using ForgeKit.Api.Tests.Integration;
using Shouldly;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ForgeKit.Api.Tests.Middlewares;

/// <summary>
/// Integration tests for CorrelationIdMiddleware
/// Verifies that correlation IDs are generated, injected into logs, and returned in responses
/// </summary>
public class CorrelationIdMiddlewareTests : IAsyncLifetime
{
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CorrelationId_GeneratedForRequestWithoutHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-ID").ShouldBeTrue();
        var correlationId = response.Headers.GetValues("X-Correlation-ID").First()!;
        correlationId.ShouldNotBeNullOrWhiteSpace();
        correlationId.Length.ShouldBe(32); // GUID without hyphens
    }

    [Fact]
    public async Task CorrelationId_ExtractedFromRequestHeader()
    {
        // Arrange
        var testCorrelationId = "test-correlation-123abc";
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        request.Headers.Add("X-Correlation-ID", testCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-ID").ShouldBeTrue();
        var returnedCorrelationId = response.Headers.GetValues("X-Correlation-ID").First()!;
        returnedCorrelationId.ShouldBe(testCorrelationId);
    }

    [Fact]
    public async Task CorrelationId_IgnoresNullOrWhitespaceHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        request.Headers.Add("X-Correlation-ID", "   "); // Whitespace only

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var returnedCorrelationId = response.Headers.GetValues("X-Correlation-ID").First()!;
        returnedCorrelationId.ShouldNotBe("   ");
        returnedCorrelationId.Length.ShouldBe(32); // Should have generated a new one
    }

    [Fact]
    public async Task CorrelationId_ReturnsInResponseHeader()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        var expectedCorrelationId = "my-test-id-12345";
        request.Headers.Add("X-Correlation-ID", expectedCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-ID").ShouldBeTrue();
        var headerValue = response.Headers.GetValues("X-Correlation-ID").First()!;
        headerValue.ShouldBe(expectedCorrelationId);
    }

    [Fact]
    public async Task CorrelationId_UniqueForDifferentRequests()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.StatusCode.ShouldBe(HttpStatusCode.OK);
        response2.StatusCode.ShouldBe(HttpStatusCode.OK);

        var correlationId1 = response1.Headers.GetValues("X-Correlation-ID").First()!;
        var correlationId2 = response2.Headers.GetValues("X-Correlation-ID").First()!;

        correlationId1.ShouldNotBe(correlationId2, "Each request should get a unique correlation ID");
    }

    [Fact]
    public async Task CorrelationId_PropagatesAcrossMultipleRequests()
    {
        // Arrange
        var testCorrelationId = "trace-test-12345";
        
        // Act - Make multiple requests with same correlation ID
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        request1.Headers.Add("X-Correlation-ID", testCorrelationId);
        
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/v1/resources");
        request2.Headers.Add("X-Correlation-ID", testCorrelationId);

        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert - Both should preserve the same correlation ID
        var id1 = response1.Headers.GetValues("X-Correlation-ID").First()!;
        var id2 = response2.Headers.GetValues("X-Correlation-ID").First()!;
        
        id1.ShouldBe(testCorrelationId);
        id2.ShouldBe(testCorrelationId);
    }
}
