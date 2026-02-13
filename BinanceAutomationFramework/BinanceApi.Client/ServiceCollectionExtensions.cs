using System.Net.Http.Headers;
using BinanceApi.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace BinanceApi.Client;

/// <summary>
/// Extension methods for registering Binance API client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Binance API client with all dependencies.
    /// </summary>
    public static IServiceCollection AddBinanceApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate options
        var optionsSection = configuration.GetSection(BinanceApiOptions.SectionName);
        var options = optionsSection.Get<BinanceApiOptions>();

        ValidateOptions(options);

        // Register configuration options for DI
        services.Configure<BinanceApiOptions>(optionsSection);

        // Register HttpClient with typed client pattern
        services.AddHttpClient<IBinanceApiClient, BinanceApiClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-rapidapi-key", options.ApiKey);
                client.DefaultRequestHeaders.Add("x-rapidapi-host", options.ApiHost);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddPolicyHandler(GetRetryPolicy(options.RetryCount))
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    /// <summary>
    /// Validates configuration options at startup (fail-fast).
    /// </summary>
    private static void ValidateOptions(BinanceApiOptions? options)
    {
        if (options == null)
        {
            throw new InvalidOperationException(
                $"Configuration section '{BinanceApiOptions.SectionName}' not found or is empty.");
        }

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException(
                "BaseUrl is required in BinanceApi configuration.", 
                nameof(options.BaseUrl));
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "ApiKey is required in BinanceApi configuration.", 
                nameof(options.ApiKey));
        }

        if (string.IsNullOrWhiteSpace(options.ApiHost))
        {
            throw new ArgumentException(
                "ApiHost is required in BinanceApi configuration.", 
                nameof(options.ApiHost));
        }

        if (options.TimeoutSeconds <= 0)
        {
            throw new ArgumentException(
                "TimeoutSeconds must be greater than zero.", 
                nameof(options.TimeoutSeconds));
        }

        if (options.RetryCount < 0)
        {
            throw new ArgumentException(
                "RetryCount cannot be negative.", 
                nameof(options.RetryCount));
        }

        // Validate BaseUrl is a valid URI
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException(
                $"BaseUrl '{options.BaseUrl}' is not a valid absolute URI.", 
                nameof(options.BaseUrl));
        }
    }

    /// <summary>
    /// Creates an exponential backoff retry policy.
    /// Handles transient errors, timeouts, and rate limiting.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx and 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // For test project, Console is fine
                    // In production, you'd inject ILogger<T> via context
                    var statusCode = outcome.Result?.StatusCode.ToString() ?? "Exception";
                    var message = outcome.Exception?.Message ?? statusCode;
                    
                    Console.WriteLine(
                        $"[Retry {retryAttempt}/{retryCount}] Waiting {timespan.TotalSeconds}s " +
                        $"before retry. Reason: {message}");
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, stays open for 30s.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    var reason = outcome.Exception?.Message ?? 
                                outcome.Result?.StatusCode.ToString() ?? 
                                "Unknown";
                    
                    Console.WriteLine(
                        $"[Circuit Breaker] OPENED for {duration.TotalSeconds}s. Reason: {reason}");
                },
                onReset: () =>
                {
                    Console.WriteLine("[Circuit Breaker] RESET - Closed and ready for requests");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("[Circuit Breaker] HALF-OPEN - Testing if service recovered");
                });
    }
}