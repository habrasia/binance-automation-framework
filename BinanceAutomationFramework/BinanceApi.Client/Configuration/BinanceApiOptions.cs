namespace BinanceApi.Client.Configuration;

/// <summary>
/// Configuration options for Binance API client.
/// </summary>
public class BinanceApiOptions
{
    public const string SectionName = "BinanceApi";

    /// <summary>
    /// Base URL (e.g., "https://binance43.p.rapidapi.com")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// RapidAPI authentication key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// RapidAPI host header
    /// </summary>
    public string ApiHost { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}