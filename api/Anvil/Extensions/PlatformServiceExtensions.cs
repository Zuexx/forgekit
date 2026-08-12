using Anvil.Domain.Services;
using Anvil.Interfaces;
using Anvil.Services;

namespace Anvil.Extensions;

/// <summary>
/// Registers the services the shared layer owns.
/// </summary>
/// <remarks>
/// Products call <see cref="AddPlatformServices"/> from their own registration entry point
/// rather than registering these individually, so a service added to the shared layer
/// reaches every product without each one editing its composition root.
/// </remarks>
public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditContext, AuditContextService>();
        services.AddScoped<SoftDeleteDomainService>();

        return services;
    }
}
