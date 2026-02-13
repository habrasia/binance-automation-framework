using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinanceApi.Reporter;

/// <summary>
/// Extension methods for registering reporter services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers both console and JSON reporters.
    /// </summary>
    public static IServiceCollection AddReporters(
        this IServiceCollection services,
        string? jsonOutputDirectory = null)
    {
        services.AddScoped<ConsoleReporter>();
        services.AddScoped<JsonReporter>(sp => 
        {
            var logger = sp.GetRequiredService<ILogger<JsonReporter>>();
            return new JsonReporter(logger, jsonOutputDirectory);
        });
        
        return services;
    }
}