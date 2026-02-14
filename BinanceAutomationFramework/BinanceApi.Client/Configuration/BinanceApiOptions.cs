namespace BinanceApi.Client.Configuration;

/// <summary>
/// Configuration options for Binance API client.
/// </summary>
public class BinanceApiOptions
{
    public const string SectionName = "BinanceApi";

    /// <summary>
    /// Base URL (e.g., "https://api.binance.com/api/v3")
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// RapidAPI authentication key (optional - not needed for public Binance API)
    /// </summary>
    public string? ApiKey { get; set; }  // ← Make nullable

    /// <summary>
    /// RapidAPI host header (optional)
    /// </summary>
    public string? ApiHost { get; set; }  // ← Make nullable

    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}