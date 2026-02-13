using Microsoft.Extensions.DependencyInjection;

namespace BinanceApi.Services;

/// <summary>
/// Extension methods for registering business logic services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all business logic services.
    /// </summary>
    public static IServiceCollection AddBinanceServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITopGainersService, TopGainersService>();
        
        return services;
    }
}