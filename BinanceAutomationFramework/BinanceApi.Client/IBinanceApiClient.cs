using BinanceApi.Client.Models;

namespace BinanceApi.Client;

/// <summary>
/// Contract for Binance API interactions.
/// </summary>
public interface IBinanceApiClient
{
    /// <summary>
    /// Retrieves 24-hour ticker price change statistics for all symbols.
    /// Endpoint: GET /ticker/24hr
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>List of ticker data for all trading pairs</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    Task<List<TickerData>> GetAllTickersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current average price for a specific symbol.
    /// Endpoint: GET /avgPrice?symbol={symbol}
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g., "ETHBTC")</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Average price data for the specified symbol</returns>
    /// <exception cref="HttpRequestException">Thrown when API request fails</exception>
    /// <exception cref="ArgumentException">Thrown when symbol is null or empty</exception>
    Task<AveragePriceData> GetAveragePriceAsync(string symbol, CancellationToken cancellationToken = default);
}