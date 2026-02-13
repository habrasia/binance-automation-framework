using BinanceApi.Client;
using BinanceApi.Services.Models;
using Microsoft.Extensions.Logging;

namespace BinanceApi.Services;

/// <summary>
/// Service implementation for identifying top gaining symbols.
/// Orchestrates calls to the API client and processes results.
/// </summary>
public class TopGainersService : ITopGainersService
{
    private readonly IBinanceApiClient _apiClient;
    private readonly ILogger<TopGainersService> _logger;

    /// <summary>
    /// Initializes a new instance of TopGainersService.
    /// </summary>
    /// <param name="apiClient">Binance API client for making requests</param>
    /// <param name="logger">Logger for diagnostic information</param>
    public TopGainersService(
        IBinanceApiClient apiClient, 
        ILogger<TopGainersService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<TopGainerResult>> GetTopGainersAsync(
        int count = 3, 
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            throw new ArgumentException("Count must be greater than zero.", nameof(count));
        }

        _logger.LogInformation("Fetching top {Count} gainers", count);

        // Step 1: Get all 24hr ticker data
        var allTickers = await _apiClient.GetAllTickersAsync(cancellationToken);
        _logger.LogDebug("Retrieved {TickerCount} tickers from API", allTickers.Count);

        // Step 2 & 3: Filter, sort, and take top N
        var topGainers = allTickers
            .Where(IsValidTicker)
            .OrderByDescending(t => t.PriceChangePercentValue)
            .Take(count)
            .ToList();

        _logger.LogInformation(
            "Identified top {Count} gainers: {Symbols}", 
            topGainers.Count, 
            string.Join(", ", topGainers.Select(t => t.Symbol)));

        // Step 4: Fetch average prices in parallel
        var results = await FetchAveragePricesAsync(topGainers, cancellationToken);

        return results;
    }

    /// <summary>
    /// Validates if a ticker has usable data.
    /// </summary>
    private static bool IsValidTicker(Client.Models.TickerData ticker) =>
        !string.IsNullOrWhiteSpace(ticker.Symbol) && 
        ticker.PriceChangePercentValue != 0;

    /// <summary>
    /// Fetches average prices for multiple symbols in parallel.
    /// </summary>
    private async Task<List<TopGainerResult>> FetchAveragePricesAsync(
        List<Client.Models.TickerData> tickers,
        CancellationToken cancellationToken)
    {
        // Create tasks for all API calls (runs in parallel!)
        var tasks = tickers.Select((ticker, index) => 
            FetchAveragePriceForSymbolAsync(ticker, index + 1, cancellationToken));

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        return results.ToList();
    }

    /// <summary>
    /// Fetches average price for a single symbol with error handling.
    /// </summary>
    private async Task<TopGainerResult> FetchAveragePriceForSymbolAsync(
        Client.Models.TickerData ticker,
        int rank,
        CancellationToken cancellationToken)
    {
        try
        {
            var avgPrice = await _apiClient.GetAveragePriceAsync(ticker.Symbol, cancellationToken);

            _logger.LogDebug(
                "Retrieved average price for {Symbol}: {Price}", 
                ticker.Symbol, 
                avgPrice.Price);

            return CreateResult(ticker, avgPrice.PriceValue, rank);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to retrieve average price for {Symbol}. Using zero as fallback.", 
                ticker.Symbol);

            return CreateResult(ticker, averagePrice: 0, rank);
        }
    }

    /// <summary>
    /// Creates a TopGainerResult from ticker data.
    /// </summary>
    private static TopGainerResult CreateResult(
        Client.Models.TickerData ticker,
        decimal averagePrice,
        int rank)
    {
        return new TopGainerResult
        {
            Symbol = ticker.Symbol,
            PriceChangePercent = ticker.PriceChangePercentValue,
            AveragePrice = averagePrice,
            LastPrice = ParseDecimalSafely(ticker.LastPrice),
            Volume = ticker.Volume,
            Rank = rank
        };
    }

    /// <summary>
    /// Safely parses a decimal string, returning 0 on failure.
    /// </summary>
    private static decimal ParseDecimalSafely(string value) =>
        decimal.TryParse(value, out var result) ? result : 0m;
}