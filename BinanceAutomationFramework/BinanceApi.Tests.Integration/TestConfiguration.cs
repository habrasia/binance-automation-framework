using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BinanceApi.Client;
using BinanceApi.Services;
using BinanceApi.Reporter;

namespace BinanceApi.Tests.Integration;

/// <summary>
/// Helper class for configuring services in integration tests.
/// Handles configuration loading from appsettings.json and environment variables.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Builds a complete configuration from appsettings.json and environment variables.
    /// </summary>
    public static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                         ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Creates a configured service provider with all dependencies registered.
    /// </summary>
    public static ServiceProvider BuildServiceProvider()
    {
        var configuration = BuildConfiguration();

        var services = new ServiceCollection();

        // Add logging with simplified configuration
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddDebug();
        });

        // Add our services
        services.AddBinanceApiClient(configuration);
        services.AddBinanceServices();
        services.AddReporters();  // ← ADD THIS LINE

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Validates that required configuration is present.
    /// Throws helpful exception if API key is missing.
    /// </summary>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var baseUrl = configuration["BinanceApi:BaseUrl"];
        
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "BaseUrl not configured. Please set BinanceApi:BaseUrl in appsettings.json");
        }
    }
}