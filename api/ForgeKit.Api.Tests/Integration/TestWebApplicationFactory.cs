using ForgeKit.Api.Data;
using ForgeKit.Api.Data.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeKit.Api.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _appConnection = CreateOpenConnection();
    private readonly SqliteConnection _authConnection = CreateOpenConnection();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure we're not in Production environment for tests
        // Use environment variable if set, otherwise default to Testing
        builder.UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Testing");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Disable HTTPS redirection in tests
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kestrel:Endpoints:Http:Url"] = "http://localhost:0",
                ["Kestrel:Endpoints:Https:Url"] = null
            });
        });
        
        builder.ConfigureLogging(logging =>
        {
            // Suppress noisy warnings in tests
            logging.AddFilter("Microsoft.AspNetCore.HttpsPolicy", LogLevel.Error);
            logging.AddFilter("MediatR", LogLevel.Error);
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove all EF Core related services for both contexts
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType.FullName?.Contains("DbContextOptions") == true ||
                d.ServiceType.FullName?.Contains("AppDbContext") == true ||
                d.ServiceType.FullName?.Contains("BetterAuthDbContext") == true ||
                d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_appConnection), ServiceLifetime.Scoped);
            
            services.AddDbContext<BetterAuthDbContext>(options =>
                options.UseSqlite(_authConnection), ServiceLifetime.Scoped);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _appConnection.Dispose();
            _authConnection.Dispose();
        }
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }
}
