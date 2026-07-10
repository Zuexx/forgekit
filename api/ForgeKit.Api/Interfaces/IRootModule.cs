namespace ForgeKit.Api.Interfaces;

/// <summary>
/// Marker interface for modules that should be mapped at the root path instead of /v1.
/// Typically used for infrastructure endpoints like health checks, metrics, etc.
/// </summary>
public interface IRootModule : IModule
{
}
