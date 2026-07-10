using ForgeKit.Api.Interfaces;
using ForgeKit.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace ForgeKit.Api.Tests.Services;

public class AuditContextServiceTests
{
    [Fact]
    public void UserId_WithValidNameIdentifierClaim_ReturnsClaimValue()
    {
        // Arrange
        var expectedUserId = "test-user-123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserId;

        // Assert
        Assert.Equal(expectedUserId, result);
    }

    [Fact]
    public void UserId_WithoutNameIdentifierClaim_ReturnsFallbackSystem()
    {
        // Arrange
        var claims = new Claim[] { };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserId;

        // Assert
        Assert.Equal("system", result);
    }

    [Fact]
    public void UserId_WithNullHttpContext_ReturnsFallbackSystem()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserId;

        // Assert
        Assert.Equal("system", result);
    }

    [Fact]
    public void UserName_WithValidNameClaim_ReturnsClaimValue()
    {
        // Arrange
        var expectedUserName = "John Doe";
        var claims = new[] { new Claim(ClaimTypes.Name, expectedUserName) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserName;

        // Assert
        Assert.Equal(expectedUserName, result);
    }

    [Fact]
    public void UserName_WithoutNameClaim_ReturnsFallbackSystem()
    {
        // Arrange
        var claims = new Claim[] { };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserName;

        // Assert
        Assert.Equal("system", result);
    }

    [Fact]
    public void UserName_WithNullHttpContext_ReturnsFallbackSystem()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var result = service.UserName;

        // Assert
        Assert.Equal("system", result);
    }

    [Fact]
    public void UtcNow_ReturnsCurrentUtcDateTime()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = service.UtcNow;

        var afterCall = DateTime.UtcNow;

        // Assert
        Assert.True(result >= beforeCall);
        Assert.True(result <= afterCall);
    }

    [Fact]
    public void UtcNow_MultipleCallsReturnIncreasingTime()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var firstCall = service.UtcNow;
        System.Threading.Thread.Sleep(10);
        var secondCall = service.UtcNow;

        // Assert
        Assert.True(secondCall >= firstCall);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var logger = Substitute.For<ILogger<AuditContextService>>();
        Assert.Throws<ArgumentNullException>(() => new AuditContextService(null!, logger));
    }

    [Fact]
    public void UserId_AndUserName_WithMultipleClaims_ReturnsCorrectValues()
    {
        // Arrange
        var userId = "user-456";
        var userName = "Jane Smith";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Email, "jane@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns(principal);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var logger = Substitute.For<ILogger<AuditContextService>>();
        var service = new AuditContextService(httpContextAccessor, logger);

        // Act
        var resultUserId = service.UserId;
        var resultUserName = service.UserName;

        // Assert
        Assert.Equal(userId, resultUserId);
        Assert.Equal(userName, resultUserName);
    }
}

