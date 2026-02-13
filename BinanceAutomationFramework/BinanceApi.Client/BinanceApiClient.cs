using System.Net.Http.Json;
using System.Text.Json;
using BinanceApi.Client.Models;

namespace BinanceApi.Client;

/// <summary>
/// Binance API client implementation via RapidAPI.
/// </summary>
public class BinanceApiClient : IBinanceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of BinanceApiClient.
    /// HttpClient is pre-configured by HttpClientFactory.
    /// </summary>
    public BinanceApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<TickerData>> GetAllTickersAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/ticker/24hr", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var tickers = await response.Content.ReadFromJsonAsync<List<TickerData>>(
            _jsonOptions, 
            cancellationToken);

        return tickers ?? [];
    }

    public async Task<AveragePriceData> GetAveragePriceAsync(
        string symbol, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var response = await _httpClient.GetAsync(
            $"/avgPrice?symbol={symbol}", 
            cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var averagePrice = await response.Content.ReadFromJsonAsync<AveragePriceData>(
            _jsonOptions, 
            cancellationToken);

        return averagePrice 
            ?? throw new InvalidOperationException($"Null response for {symbol}");
    }
}