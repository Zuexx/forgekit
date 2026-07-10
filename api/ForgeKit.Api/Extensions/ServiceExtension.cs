using ForgeKit.Api.Domain.Services;
using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Services;
using ForgeKit.Api.Services.Todos;

namespace ForgeKit.Api.Extensions;

public static class ServiceExtension
{
    public static IServiceCollection RegisterApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<TodoService>();

        return services;
    }

    public static IServiceCollection RegisterAuditContext(
        this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditContext, AuditContextService>();

        return services;
    }

    public static IServiceCollection RegisterDomainServices(
        this IServiceCollection services)
    {
        services.AddScoped<SoftDeleteDomainService>();

        return services;
    }
}
