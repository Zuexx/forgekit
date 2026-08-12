using Anvil.Data;
using Anvil.Interfaces;
using ForgeKit.Api.Data;
using ForgeKit.Api.Services.Todos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Integration;

/// <summary>
/// Asserts that services resolved by optional lookup are actually registered.
/// </summary>
/// <remarks>
/// Services fetched with GetService rather than GetRequiredService fail silently when
/// their registration is missing or its type changes. IDataSeeder is the case that
/// motivated these tests: StartSeed resolves it optionally, so an unregistered seeder
/// disables seeding without any error, in a code path that only runs outside tests.
/// </remarks>
public class CompositionRootTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CompositionRootTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void DataSeeder_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();

        scope.ServiceProvider.GetService<IDataSeeder>().ShouldNotBeNull();
    }

    [Fact]
    public void UnitOfWork_ResolvesForTheProductContext()
    {
        using var scope = _factory.Services.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<AppDbContext>>();

        unitOfWork.ShouldNotBeNull();
        unitOfWork.DbContext.ShouldBeOfType<AppDbContext>();
    }

    [Fact]
    public void PlatformServices_AreRegisteredThroughTheProductEntryPoint()
    {
        using var scope = _factory.Services.CreateScope();

        scope.ServiceProvider.GetService<IAuditContext>().ShouldNotBeNull();
        scope.ServiceProvider.GetService<Anvil.Domain.Services.SoftDeleteDomainService>().ShouldNotBeNull();
    }

    [Fact]
    public void ProductServices_AreRegistered()
    {
        using var scope = _factory.Services.CreateScope();

        scope.ServiceProvider.GetService<TodoService>().ShouldNotBeNull();
    }
}
