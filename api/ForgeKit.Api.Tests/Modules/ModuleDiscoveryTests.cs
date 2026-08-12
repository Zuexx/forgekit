using System.Net;
using Anvil.Extensions;
using Anvil.Interfaces;
using Shouldly;
using Xunit;

namespace ForgeKit.Api.Tests.Modules;

/// <summary>
/// Guards module discovery across the shared/product assembly boundary.
/// </summary>
/// <remarks>
/// Module discovery once scanned only the assembly declaring IModule. After that
/// interface moved into the shared layer, product modules stopped being discovered
/// and their endpoints silently disappeared — the application still started and
/// still built. These tests exist so that failure mode cannot return unnoticed.
/// </remarks>
public class ModuleDiscoveryTests : IClassFixture<Integration.TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ModuleDiscoveryTests(Integration.TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public void Discovery_FindsModules()
    {
        ModuleExtensions.RegisteredModuleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Discovery_FindsModulesFromBothAssemblies()
    {
        var moduleInterface = typeof(IModule);

        var sharedModules = moduleInterface.Assembly.GetTypes()
            .Count(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(moduleInterface));
        var productModules = typeof(Program).Assembly.GetTypes()
            .Count(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(moduleInterface));

        sharedModules.ShouldBeGreaterThan(0);
        productModules.ShouldBeGreaterThan(0);
        ModuleExtensions.RegisteredModuleCount.ShouldBeGreaterThanOrEqualTo(sharedModules + productModules);
    }

    [Fact]
    public async Task Discovery_MapsProductEndpoints()
    {
        var response = await _client.GetAsync("/v1/resources");

        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Discovery_MapsSharedEndpoints()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }
}
