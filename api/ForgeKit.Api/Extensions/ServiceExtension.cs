using Anvil.Extensions;
using Anvil.Interfaces;
using ForgeKit.Api.Foundations;
using ForgeKit.Api.Services.Todos;

namespace ForgeKit.Api.Extensions;

/// <summary>
/// Registers this product's services, and pulls in the shared layer's registrations.
/// </summary>
public static class ServiceExtension
{
    public static IServiceCollection RegisterApplicationServices(
        this IServiceCollection services)
    {
        services.AddPlatformServices();

        services.AddScoped<TodoService>();
        services.AddTransient<IDataSeeder, PocDataSeeder>();

        return services;
    }
}
