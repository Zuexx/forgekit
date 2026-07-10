using System.Security.Claims;
using ForgeKit.Api.Models;

namespace ForgeKit.Api.Extensions;

public static class HttpContextAccessorExtension
{
    public static AuthorizedUser Current(this IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return new AuthorizedUser
            {
                Name = "System.Name",
                Id = "System.Id"
            };
            //throw new UnauthorizedAccessException("User is not authenticated or HttpContext is null.");
        }

        var userName = user.FindFirstValue("name");
        // var userEmail = user.Identity?.Name;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userName) ||
        // string.IsNullOrEmpty(userEmail) ||
        string.IsNullOrEmpty(userId))
        {
            throw new BadHttpRequestException("User name is not available.");
        }


        return new AuthorizedUser
        {
            Name = userName,
            // Email = userEmail,
            Id = userId
        };
    }
}