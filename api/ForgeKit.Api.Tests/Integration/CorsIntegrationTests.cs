using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace ForgeKit.Api.Tests.Integration;

public sealed class CorsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private const string DevelopmentOrigin = "http://localhost:3000";
    private const string ProductionOrigin = "https://myapp.com";

    private readonly TestWebApplicationFactory _factory;

    public CorsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DevelopmentCors_ShouldReturnHeadersForCrossOriginRequest()
    {
        using var client = CreateClient("Development");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", DevelopmentOrigin);

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("*");
    }

    [Fact]
    public async Task DevelopmentCors_ShouldHandlePreflightOptionsRequest()
    {
        using var client = CreateClient("Development");
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        request.Headers.Add("Origin", DevelopmentOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe("*");
        response.Headers.GetValues("Access-Control-Allow-Methods").Single().ShouldContain("GET");
    }

    [Fact]
    public async Task ProductionCors_ShouldAllowConfiguredOrigin()
    {
        using var client = CreateClient("Production");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", ProductionOrigin);

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(ProductionOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Single().ShouldBe("true");
    }

    [Fact]
    public async Task ProductionCors_ShouldRejectUnconfiguredOrigin()
    {
        using var client = CreateClient("Production");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://blocked.example");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    private HttpClient CreateClient(string environment)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
