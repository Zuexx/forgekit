namespace ForgeKit.Api.Interfaces;

/// <summary>
/// Marker interface for sample/demo modules that should only be available in non-production environments.
/// Modules implementing this interface will be automatically excluded when IsProduction() == true.
/// </summary>
public interface ISampleModule : IModule
{
}
