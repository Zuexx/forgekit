using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Foundations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;

namespace ForgeKit.Api.Extensions;

public static class ModuleExtensions
{
    // Static list shared across all instances - populated on first registration
    private static readonly List<IModule> registeredModules = [];
    private static bool isInitialized = false;
    private static readonly object lockObject = new();

    public static IServiceCollection RegisterModules(this IServiceCollection services)
    {
        lock (lockObject)
        {
            // Only discover and register modules once globally
            if (!isInitialized)
            {
                var modules = DiscoverModules();
                foreach (var module in modules)
                {
                    module.RegisterModule(services);
                    registeredModules.Add(module);
                }
                isInitialized = true;
            }
            else
            {
                // Re-register services for new service collection (e.g., in tests)
                foreach (var module in registeredModules)
                {
                    module.RegisterModule(services);
                }
            }
        }
        return services;
    }
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // Filter out sample modules in production
        var modulesToMap = app.Environment.IsProduction()
            ? registeredModules.Where(m => m is not ISampleModule)
            : registeredModules;

        // Map root modules (e.g., health checks) at root path
        var rootModules = modulesToMap.Where(m => m is IRootModule);
        foreach (var module in rootModules)
        {
            module.MapEndpoints(app);
        }

        // Map versioned API modules under /v1
        var versionedModules = modulesToMap.Where(m => m is not IRootModule);
        var v1Endpoints = app.MapGroup("/v1");
        foreach (var module in versionedModules)
        {
            module.MapEndpoints(v1Endpoints);
        }

        return app;
    }
    public static WebApplication StartSeed(this WebApplication app)
    {
        var scopedFactory = app.Services.GetService<IServiceScopeFactory>();
        if (scopedFactory == null)
            return app;

        using (var scope = scopedFactory.CreateScope())
        {
            try
            {
                var config = scope.ServiceProvider.GetService<IConfiguration>();
                var enabledFlag = config?.GetValue<bool>("SeedEnabled") ?? false;
                var allow = app.Environment.IsDevelopment() || enabledFlag;
                if (!allow)
                    return app;

                var seeder = scope.ServiceProvider.GetService<PocDataSeeder>();
                seeder?.Seed();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PocDataSeeder failed: {ex.Message}");
            }
        }
        return app;
    }
    private static IEnumerable<IModule> DiscoverModules()
    {
        return typeof(IModule).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(IModule)))
            .Select(Activator.CreateInstance)
            .Cast<IModule>();
    }
}