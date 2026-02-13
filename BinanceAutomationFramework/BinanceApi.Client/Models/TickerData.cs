using System.Text.Json.Serialization;

namespace BinanceApi.Client.Models;

/// <summary>
/// Represents 24-hour ticker price change statistics for a trading symbol.
/// Maps to the response from GET /ticker/24hr endpoint.
/// </summary>
public class TickerData
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("priceChange")]
    public string PriceChange { get; set; } = string.Empty;

    [JsonPropertyName("priceChangePercent")]
    public string PriceChangePercent { get; set; } = string.Empty;

    [JsonPropertyName("weightedAvgPrice")]
    public string WeightedAvgPrice { get; set; } = string.Empty;

    [JsonPropertyName("lastPrice")]
    public string LastPrice { get; set; } = string.Empty;

    [JsonPropertyName("openPrice")]
    public string OpenPrice { get; set; } = string.Empty;

    [JsonPropertyName("highPrice")]
    public string HighPrice { get; set; } = string.Empty;

    [JsonPropertyName("lowPrice")]
    public string LowPrice { get; set; } = string.Empty;

    [JsonPropertyName("volume")]
    public string Volume { get; set; } = string.Empty;

    [JsonPropertyName("closeTime")]
    public long CloseTime { get; set; }

    /// <summary>
    /// Price change percentage as decimal. Returns 0 if invalid.
    /// </summary>
    [JsonIgnore]
    public decimal PriceChangePercentValue =>
        decimal.TryParse(PriceChangePercent, out var result) ? result : 0m;
}