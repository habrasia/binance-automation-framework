using BinanceApi.Services.Models;

namespace BinanceApi.Reporter;

/// <summary>
/// Contract for formatting and presenting top gainer results.
/// </summary>
public interface IResultReporter
{
    /// <summary>
    /// Reports the top gainers results in a specific format.
    /// </summary>
    /// <param name="results">List of top gainer results to report</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReportAsync(List<TopGainerResult> results, CancellationToken cancellationToken = default);
}