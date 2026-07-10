# Design: CORS and Health Check Implementation

**Version**: 1.0  
**Last Updated**: 2026-02-11

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         Request Flow                             │
├─────────────────────────────────────────────────────────────────┤
│  Browser/Client                                                  │
│       │                                                          │
│       ↓                                                          │
│  ┌─────────────────────────────────────────┐                    │
│  │  CORS Middleware                        │                    │
│  │  • Check Origin                         │                    │
│  │  • Add CORS headers                     │                    │
│  │  • Handle preflight                     │                    │
│  └─────────────────────────────────────────┘                    │
│       ↓                                                          │
│  Authentication → Authorization                                  │
│       ↓                                                          │
│  Endpoint Routing (IModule pattern)                             │
│  ├─ ResourcesModule → /api/v1/resources                         │
│  └─ HealthModule → /health, /health/ready, /health/live         │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. CORS Design

### 2.1 CORS Extension Architecture

```csharp
// Api/Extensions/CorsExtensions.cs
namespace Api.Extensions;

public static class CorsExtensions
{
    private const string DefaultPolicyName = "DefaultCorsPolicy";
    
    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();
        
        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                if (environment.IsDevelopment())
                {
                    // Development: Allow any origin
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
                else
                {
                    // Production: Strict whitelist
                    if (allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "Production requires Cors:AllowedOrigins");
                    }
                    
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromHours(1));
                }
            });
        });
        
        return services;
    }
    
    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(DefaultPolicyName);
        return app;
    }
}
```

### 2.2 Configuration Structure

```json
// appsettings.json (Production)
{
  "Cors": {
    "AllowedOrigins": [
      "https://myapp.com",
      "https://www.myapp.com"
    ]
  }
}

// appsettings.Development.json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:4200",
      "http://localhost:5173"
    ]
  }
}
```

---

## 3. Health Check Design

### 3.1 Health Module Architecture (IModule Pattern)

```csharp
// Api/Modules/HealthModule.cs
namespace Api.Modules;

public class HealthModule : IModule
{
    public void RegisterEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Overall health check
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
        .WithTags("Health")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get overall API health status";
            return operation;
        });
        
        // Readiness probe
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
        .WithTags("Health")
        .WithOpenApi();
        
        // Liveness probe
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false  // No checks, always 200
        })
        .WithTags("Health")
        .WithOpenApi();
    }
}
```

### 3.2 Health Check Service Registration

```csharp
// Program.cs
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "appdbcontext",
        tags: new[] { "ready" });
```

### 3.3 Response Format

**Healthy** (200 OK):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "appdbcontext": {
      "status": "Healthy",
      "duration": "00:00:00.0567890",
      "tags": ["ready"]
    }
  }
}
```

**Unhealthy** (503 Service Unavailable):
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:05.0000000",
  "entries": {
    "appdbcontext": {
      "status": "Unhealthy",
      "description": "Unable to connect to database",
      "duration": "00:00:05.0000000",
      "tags": ["ready"]
    }
  }
}
```

---

## 4. Integration with Program.cs

```csharp
// Program.cs
using Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ... Existing services ...

// Add CORS
builder.Services.AddCorsPolicy(
    builder.Configuration,
    builder.Environment);

// Add Health Checks
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(tags: new[] { "ready" });

// ... Register modules (HealthModule auto-discovered) ...
builder.Services.RegisterModules();

var app = builder.Build();

// CRITICAL: CORS must be before Authentication
app.UseCorsPolicy();
app.UseAuthentication();
app.UseAuthorization();

// Map all module endpoints (including HealthModule)
app.MapEndpoints();

app.Run();
```

**Middleware Order**:
1. `app.UseHttpsRedirection()`
2. `app.UseMiddleware<CorrelationIdMiddleware>()`
3. `app.UseCorsPolicy()` ← **CORS (before auth!)**
4. `app.UseAuthentication()`
5. `app.UseAuthorization()`
6. `app.UseMiddleware<ExceptionHandlingMiddleware>()`
7. `app.MapEndpoints()`

---

## 5. File Structure

```
Api/
├── Extensions/
│   └── CorsExtensions.cs          ← New file
├── Modules/
│   ├── SampleResourceModule.cs    ← Existing
│   └── HealthModule.cs            ← New file (IModule pattern)
├── Program.cs                     ← Modified
├── appsettings.json               ← Modified
└── appsettings.Development.json   ← Modified

Api.Tests/
└── Integration/
    ├── Cors/
    │   └── CorsIntegrationTests.cs       ← New file
    └── Modules/
        └── HealthModuleTests.cs          ← New file
```

---

## 6. Testing Strategy

### 6.1 CORS Tests

```csharp
// Api.Tests/Integration/Cors/CorsIntegrationTests.cs
public class CorsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetRequest_WithAllowedOrigin_ReturnsCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        
        // Act
        var response = await client.GetAsync("/api/v1/resources");
        
        // Assert
        response.Headers.ShouldContain(h => 
            h.Key == "Access-Control-Allow-Origin");
    }
}
```

### 6.2 Health Check Tests

```csharp
// Api.Tests/Integration/Modules/HealthModuleTests.cs
public class HealthModuleTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthEndpoint_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Healthy");
    }
}
```

---

## 7. Kubernetes Integration

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: scaffold-api
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
```

---

## 8. Performance & Security

**CORS Performance**:
- Negligible overhead (<1ms)
- Preflight cached for 1 hour

**Health Check Performance**:
- `/health/live`: <1ms (no checks)
- `/health/ready`: 10-100ms (DB check)
- `/health`: 10-100ms (all checks)

**Security**:
- Never use `AllowAnyOrigin()` in production
- Always validate production has `AllowedOrigins`
- Health endpoints unauthenticated (for load balancers)

---

## 9. Package Dependencies

```xml
<!-- New dependency -->
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" 
                  Version="10.0.0" />

<!-- Built-in (no new packages needed) -->
<!-- Microsoft.AspNetCore.Cors -->
<!-- Microsoft.Extensions.Diagnostics.HealthChecks -->
```

---

## 10. Summary

**CORS**:
- Extension method pattern
- Environment-aware (Dev vs Prod)
- Configuration-driven

**Health Checks**:
- **IModule pattern** (HealthModule)
- Auto-discovered by `RegisterModules()`
- Three endpoints: `/health`, `/health/ready`, `/health/live`
- Database health check with `ready` tag

**Integration**:
- CORS before Authentication (critical!)
- Health module auto-registered
- Zero breaking changes to existing code
