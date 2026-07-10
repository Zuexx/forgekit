using ForgeKit.Api.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using AspNetResults = Microsoft.AspNetCore.Http.Results;

namespace ForgeKit.Api.Modules;

/// <summary>
/// Health Check Module providing endpoints for monitoring API health status.
/// Implements three endpoints following Kubernetes probe patterns:
/// - /health: Overall health (aggregate of all checks)
/// - /health/ready: Readiness probe (can serve traffic?)
/// - /health/live: Liveness probe (is process alive?)
/// 
/// Note: Implements IRootModule to map endpoints at root path (not under /v1).
/// </summary>
public class HealthModule : IRootModule
{
    public IServiceCollection RegisterModule(IServiceCollection services)
    {
        // Health checks are registered in Program.cs
        // No additional registration needed here
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/health")
            .WithTags("Health");

        // GET /health - Overall health check
        group.MapGet("/", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                entries = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.ToString(),
                        description = entry.Value.Description,
                        tags = entry.Value.Tags
                    })
            };

            var statusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
            return AspNetResults.Json(response, statusCode: statusCode);
        })
        .WithName("GetHealth")
        .WithSummary("Get overall API health status")
        .WithDescription("Returns the health status of all registered health checks including database connectivity.")
        .AllowAnonymous();

        // GET /health/ready - Readiness probe
        group.MapGet("/ready", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));
            
            var response = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.ToString(),
                entries = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.ToString(),
                        description = entry.Value.Description,
                        tags = entry.Value.Tags
                    })
            };

            var statusCode = report.Status == HealthStatus.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
            return AspNetResults.Json(response, statusCode: statusCode);
        })
        .WithName("GetHealthReady")
        .WithSummary("Readiness probe for load balancers")
        .WithDescription("Returns 200 if API is ready to serve traffic (database connected, etc.).")
        .AllowAnonymous();

        // GET /health/live - Liveness probe
        group.MapGet("/live", () =>
        {
            var response = new
            {
                status = "Healthy",
                message = "Process is alive"
            };
            return AspNetResults.Json(response);
        })
        .WithName("GetHealthLive")
        .WithSummary("Liveness probe for Kubernetes")
        .WithDescription("Returns 200 OK if the process is alive (no health checks performed).")
        .AllowAnonymous();

        return endpoints;
    }
}
