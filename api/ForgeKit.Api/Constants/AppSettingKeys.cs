namespace ForgeKit.Api.Constants;

/// <summary>
/// Configuration keys for reading settings from appsettings.json.
/// </summary>
/// <remarks>
/// These constants are used with IConfiguration to access application settings.
/// Centralizing these keys ensures consistency and makes refactoring easier.
/// </remarks>
public static class AppSettingKeys
{
    /// <summary>
    /// Key for JWT configuration (Issuer, Audience).
    /// </summary>
    public const string JwtData = "JwtData";

    /// <summary>
    /// Key for SAS URI settings (Azure Blob Storage).
    /// </summary>
    public const string SasUriSettings = "SasUriSettings";

    /// <summary>
    /// Key for JWKS (JSON Web Key Set) callback URL.
    /// Used for JWT token validation with external JWKS endpoint.
    /// </summary>
    public const string Jwks = "JwksCallBackUrl:Jwks";

    /// <summary>
    /// Key for controlling whether database seeding is enabled.
    /// Used in development/testing environments to populate initial data.
    /// </summary>
    public const string SeedEnabled = "SeedEnabled";
}
