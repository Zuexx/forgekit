using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Integration;

/// <summary>
/// The OpenAPI document is what the Scalar reference renders, and it is only mapped in
/// Development, so no other test requests it.
/// </summary>
/// <remarks>
/// <c>Microsoft.AspNetCore.OpenApi</c> and <c>Microsoft.OpenApi</c> are versioned separately and
/// bound at runtime: an incompatible pair still compiles, and the document endpoint then fails
/// with a <see cref="MissingMethodException"/> on the first request. This test is the only
/// thing that catches that pairing (docs/DEPENDENCY_CONSTRAINTS.md, Microsoft.OpenApi).
/// </remarks>
public class OpenApiDocumentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public OpenApiDocumentTests(TestWebApplicationFactory factory)
    {
        _client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"))
            .CreateClient();
    }

    [Fact]
    public async Task OpenApiDocument_IsGeneratedInDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("paths").EnumerateObject()
            .ShouldContain(p => p.Name.StartsWith("/v1/resources"));
        document.RootElement.GetProperty("components").GetProperty("securitySchemes")
            .TryGetProperty("Bearer", out _).ShouldBeTrue();
    }
}
