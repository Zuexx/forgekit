using System.Reflection;
using Anvil.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;

namespace Anvil.Extensions;

public static class ModuleExtensions
{
    // Static list shared across all instances - populated on first registration
    private static readonly List<IModule> registeredModules = [];
    private static bool isInitialized = false;
    private static readonly object lockObject = new();

    /// <summary>
    /// Discovers and registers modules from the shared layer plus the supplied assemblies.
    /// </summary>
    /// <remarks>
    /// Products MUST pass their own assembly. Scanning only the assembly that declares
    /// IModule would find the shared layer's modules and silently miss every product
    /// module, mapping no product endpoints at runtime.
    /// </remarks>
    public static IServiceCollection RegisterModules(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        lock (lockObject)
        {
            // Only discover and register modules once globally
            if (!isInitialized)
            {
                var modules = DiscoverModules(assemblies);
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

                var seeder = scope.ServiceProvider.GetService<IDataSeeder>();
                seeder?.Seed();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Data seeding failed: {ex.Message}");
            }
        }
        return app;
    }
    private static IEnumerable<IModule> DiscoverModules(Assembly[] assemblies)
    {
        // The shared layer is always scanned so its own modules are registered without
        // every product having to opt in.
        var scanTargets = assemblies
            .Append(typeof(IModule).Assembly)
            .Distinct()
            .ToArray();

        return scanTargets
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IModule)))
            .Select(Activator.CreateInstance)
            .Cast<IModule>();
    }

    /// <summary>
    /// Number of modules registered by the last discovery pass. Exposed so a product can
    /// assert that discovery actually found something.
    /// </summary>
    public static int RegisteredModuleCount
    {
        get { lock (lockObject) { return registeredModules.Count; } }
    }
}