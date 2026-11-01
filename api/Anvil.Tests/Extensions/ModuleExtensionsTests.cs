using Anvil.Extensions;
using Anvil.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Anvil.Tests.Extensions;

/// <remarks>
/// A product that called RegisterModules without its own assembly used to start normally
/// and map none of its endpoints. These tests hold the call to failing at startup instead.
/// The checks run before discovery, so they leave the process-wide module registry alone.
/// </remarks>
public sealed class ModuleExtensionsTests
{
    [Fact]
    public void RegisterModules_WithoutAnyAssembly_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() => new ServiceCollection().RegisterModules());

        ex.Message.ShouldContain("typeof(Program).Assembly");
    }

    [Fact]
    public void RegisterModules_WithOnlyTheSharedAssembly_Throws()
    {
        Should.Throw<ArgumentException>(
            () => new ServiceCollection().RegisterModules(typeof(IModule).Assembly));
    }
}
