namespace BinanceApi.Services.Models;

/// <summary>
/// Represents a top gaining trading symbol with its statistics.
/// Combines ticker data with average price information.
/// </summary>
public record TopGainerResult
{
    /// <summary>
    /// Trading pair symbol (e.g., "ETHBTC")
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 24-hour price change percentage (e.g., 2.5 means +2.5%)
    /// </summary>
    public decimal PriceChangePercent { get; set; }

    /// <summary>
    /// Current average price over the last 5 minutes
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Last traded price from 24hr ticker
    /// </summary>
    public decimal LastPrice { get; set; }

    /// <summary>
    /// Trading volume in the last 24 hours
    /// </summary>
    public string Volume { get; set; } = string.Empty;

    /// <summary>
    /// Rank position (1 = highest gainer, 2 = second, 3 = third)
    /// </summary>
    public int Rank { get; set; }
}