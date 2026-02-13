using BinanceApi.Services.Models;

namespace BinanceApi.Services;

/// <summary>
/// Service for identifying and retrieving top gaining trading symbols.
/// </summary>
public interface ITopGainersService
{
    /// <summary>
    /// Identifies the top N trading symbols by 24-hour price change percentage
    /// and retrieves their average prices.
    /// </summary>
    /// <param name="count">Number of top gainers to retrieve (default: 3)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top gainer results, ordered by rank</returns>
    Task<List<TopGainerResult>> GetTopGainersAsync(int count = 3, CancellationToken cancellationToken = default);
}