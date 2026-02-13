using System.Text.Json.Serialization;

namespace BinanceApi.Client.Models;

/// <summary>
/// Represents the current average price for a symbol.
/// Maps to the response from GET /avgPrice endpoint.
/// </summary>
public class AveragePriceData
{
    [JsonPropertyName("mins")]
    public int Mins { get; set; }

    [JsonPropertyName("price")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("closeTime")]
    public long CloseTime { get; set; }

    /// <summary>
    /// Average price as decimal. Returns 0 if invalid.
    /// </summary>
    [JsonIgnore]
    public decimal PriceValue =>
        decimal.TryParse(Price, out var result) ? result : 0m;
}