using ForgeKit.Api.Behaviors;
using ForgeKit.Api.Constants;
using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using ForgeKit.Api.Extensions;
using ForgeKit.Api.Foundations;
using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Middlewares;
using ForgeKit.Api.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Configure Serilog for structured logging
builder.Host.UseSerilog((context, configuration) =>
{
    var logConfig = configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ForgeKit.Api")
        .MinimumLevel.Override("Microsoft.AspNetCore.HttpsPolicy", Serilog.Events.LogEventLevel.Error);
    
    // In non-production environments, suppress third-party license warnings
    if (!context.HostingEnvironment.IsProduction())
    {
        logConfig.Filter.ByExcluding(logEvent => 
            logEvent.MessageTemplate.Text.Contains("license", StringComparison.OrdinalIgnoreCase) && 
            logEvent.Level == Serilog.Events.LogEventLevel.Warning);
    }
});

builder.Services.AddConfiguredDbContexts(builder.Configuration, builder.Environment);

// Dependency Injection - Data Layer
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Dependency Injection - Audit Context
builder.Services.RegisterAuditContext();

// Dependency Injection - Domain Services
builder.Services.RegisterDomainServices();

// Dependency Injection - Application Services
builder.Services.RegisterApplicationServices();

// Bind and register your JWT configuration from appsettings:
builder.Services.Configure<JwtSetupData>(
    builder.Configuration.GetSection(AppSettingKeys.JwtData));

// Register the JWKS provider (it uses lazy loading so there's no need to force initialization):
builder.Services.AddSingleton<IJwksProvider>(
    sp => new JwksProvider(builder.Configuration[AppSettingKeys.Jwks]!));

// Register our custom configuration for JwtBearer options:
builder.Services.AddTransient<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();

// Register Authentication:
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

// Register Authorization services
builder.Services.AddAuthorization();

// Register MediatR service
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register FluentValidation service
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Register MediatR pipeline behaviors (validation)
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Register exception handling middleware for DI
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// Register correlation ID middleware for DI
builder.Services.AddTransient<CorrelationIdMiddleware>();

// Dependency Injection For HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register endpoint related modules
builder.Services.RegisterModules();

// Register PocDataSeeder used for development/CI seeding (guarded by configuration)
builder.Services.AddTransient<PocDataSeeder>();

// Configure CORS policy (environment-aware: Dev allows any, Prod uses whitelist)
builder.Services.AddCorsPolicy(builder.Configuration, builder.Environment);

// Register Health Checks with database connectivity check
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "appdbcontext",
        tags: new[] { "ready", "database" });

// Limit DB health check to 5 seconds — avoids long hangs when DB is unreachable
builder.Services.Configure<HealthCheckServiceOptions>(options =>
{
    var reg = options.Registrations.FirstOrDefault(r => r.Name == "appdbcontext");
    if (reg != null) reg.Timeout = TimeSpan.FromSeconds(5);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT Bearer token"
            };

        return Task.CompletedTask;
    });

    // only add security to endpoints with [Authorize]
    options.AddOperationTransformer((operation, context, ct) =>
    {
        var requiresAuth =
            context
                .Description?
                .ActionDescriptor?
                .EndpointMetadata?
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any() == true;
        if (!requiresAuth) return Task.CompletedTask;

        operation.Security ??= [];
        var securityScheme = new OpenApiSecuritySchemeReference("Bearer", context.Document);
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [securityScheme] = []
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Scalar UI
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("API (.NET 10 + Scalar)")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Register correlation ID middleware (before authentication/authorization for full tracing)
app.UseMiddleware<CorrelationIdMiddleware>();

// Enable CORS policy (MUST be before Authentication for proper header handling)
app.UseCorsPolicy();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Map module endpoints
app.MapEndpoints();

app.StartSeed();
app.Run();
