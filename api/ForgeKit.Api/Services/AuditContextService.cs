using System.Security.Claims;
using ForgeKit.Api.Interfaces;

namespace ForgeKit.Api.Services;

/// <summary>
/// Implementation of IAuditContext that extracts user identity from HTTP context claims.
/// </summary>
/// <remarks>
/// This service is scoped, meaning one instance is created per HTTP request.
/// It uses IHttpContextAccessor to access the current HttpContext and extract JWT claims.
/// </remarks>
public class AuditContextService(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditContextService> logger) : IAuditContext
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly ILogger<AuditContextService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Extracts the user ID from ClaimTypes.NameIdentifier claim.
    /// Falls back to "system" if no authenticated user or claim is missing.
    /// </summary>
    public string UserId
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier);
                var userId = claim?.Value ?? "system";
                
                if (userId != "system")
                {
                    _logger.LogDebug("Audit context user ID extracted: {UserId}", userId);
                }
                
                return userId;
            }
            catch (Exception ex)
            {
                // If any error occurs accessing HttpContext, fall back to system
                _logger.LogWarning(ex, "Error extracting user ID from claims, falling back to 'system'");
                return "system";
            }
        }
    }

    /// <summary>
    /// Extracts the user name from ClaimTypes.Name claim.
    /// Falls back to "system" if no authenticated user or claim is missing.
    /// </summary>
    public string UserName
    {
        get
        {
            try
            {
                var claim = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.Name);
                var userName = claim?.Value ?? "system";
                
                if (userName != "system")
                {
                    _logger.LogDebug("Audit context user name extracted: {UserName}", userName);
                }
                
                return userName;
            }
            catch (Exception ex)
            {
                // If any error occurs accessing HttpContext, fall back to system
                _logger.LogWarning(ex, "Error extracting user name from claims, falling back to 'system'");
                return "system";
            }
        }
    }

    /// <summary>
    /// Returns the current UTC timestamp.
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}
