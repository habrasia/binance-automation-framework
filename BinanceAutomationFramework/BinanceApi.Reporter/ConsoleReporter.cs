using System.Text;
using BinanceApi.Services.Models;
using Microsoft.Extensions.Logging;

namespace BinanceApi.Reporter;

/// <summary>
/// Formats and outputs results to the console with color-coded formatting.
/// </summary>
public class ConsoleReporter : IResultReporter
{
    private readonly ILogger<ConsoleReporter> _logger;

    public ConsoleReporter(ILogger<ConsoleReporter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ReportAsync(List<TopGainerResult> results, CancellationToken cancellationToken = default)
    {
        if (results == null || results.Count == 0)
        {
            Console.WriteLine("No results to report.");
            return Task.CompletedTask;
        }

        var report = BuildReport(results);
        Console.WriteLine(report);
        
        _logger.LogInformation("Reported {Count} top gainers to console", results.Count);
        
        return Task.CompletedTask;
    }

    private static string BuildReport(List<TopGainerResult> results)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("           TOP CRYPTO GAINERS - LAST 24 HOURS");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var result in results.OrderBy(r => r.Rank))
        {
            sb.AppendLine($"🏆 RANK #{result.Rank}: {result.Symbol}");
            sb.AppendLine($"   Price Change:    {result.PriceChangePercent:+0.00;-0.00}%");
            sb.AppendLine($"   Average Price:   {result.AveragePrice:N8}");
            sb.AppendLine($"   Last Price:      {result.LastPrice:N8}");
            sb.AppendLine($"   24h Volume:      {result.Volume}");
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Report generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }
}