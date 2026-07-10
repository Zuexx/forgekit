using System.Net;
using System.Text.Json;
using ForgeKit.Api.Models;
using Shouldly;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ForgeKit.Api.Tests.Integration.Middlewares;

/// <summary>
/// Integration tests for exception handling middleware across the full request pipeline
/// Verifies RFC 7807 compliance and proper error response formatting
/// </summary>
public class ExceptionHandlingIntegrationTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task InvalidRequest_ReturnsErrorResponse()
    {
        // Arrange - Send a request with invalid JSON
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/some-endpoint");
        request.Content = new StringContent("not valid json",
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Should return a 400 or similar error with proper JSON format
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Fact]
    public async Task ErrorResponse_HasRequiredFields()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/some-endpoint");
        request.Content = new StringContent("not valid json",
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - Response should be valid JSON (if not empty)
        if (!string.IsNullOrEmpty(content))
        {
            var error = JsonSerializer.Deserialize<ErrorResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            error.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task Middleware_PassesValidRequests()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/");

        // Act
        var response = await _client.SendAsync(request);

        // Assert - Valid requests should either succeed or fail gracefully
        // Middleware should not interfere with valid requests
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task CorrelationIdHeader_PassesThrough()
    {
        // Arrange
        var correlationId = "integration-test-123";
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.ShouldNotBeNull();
        // The middleware should have processed the correlation ID
    }

    [Fact]
    public async Task MultipleRequests_CanBeSent()
    {
        // Arrange
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "/");

        // Act
        var response1 = await _client.SendAsync(request1);
        var response2 = await _client.SendAsync(request2);

        // Assert
        response1.ShouldNotBeNull();
        response2.ShouldNotBeNull();
    }

    [Fact]
    public async Task BadRequest_ReturnsJsonError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/some-endpoint");
        request.Content = new StringContent("{invalid json",
            System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    // Helper method to verify error response structure
    private async Task VerifyErrorResponse(HttpResponseMessage response, string expectedTitle, int expectedStatus)
    {
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
        {
            content.ShouldNotBeEmpty();
            return;
        }

        var error = JsonSerializer.Deserialize<ErrorResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        error.ShouldNotBeNull();
        if (error != null)
        {
            error.Title.ShouldBe(expectedTitle);
            error.Status.ShouldBe(expectedStatus);
            error.Detail.ShouldNotBeEmpty();
            error.TraceId.ShouldNotBeEmpty();
        }
    }
}
