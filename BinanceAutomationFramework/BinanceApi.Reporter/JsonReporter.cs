using System.Text.Json;
using BinanceApi.Services.Models;
using Microsoft.Extensions.Logging;

namespace BinanceApi.Reporter;

/// <summary>
/// Formats and saves results as JSON file.
/// </summary>
public class JsonReporter : IResultReporter
{
    private readonly ILogger<JsonReporter> _logger;
    private readonly string _outputDirectory;

    public JsonReporter(ILogger<JsonReporter> logger, string? outputDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputDirectory = outputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "results");
    }

    public async Task ReportAsync(List<TopGainerResult> results, CancellationToken cancellationToken = default)
    {
        if (results == null || results.Count == 0)
        {
            _logger.LogWarning("No results to report");
            return;
        }

        // Ensure output directory exists
        Directory.CreateDirectory(_outputDirectory);

        // Create filename with timestamp
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"top_gainers_{timestamp}.json";
        var filepath = Path.Combine(_outputDirectory, filename);

        // Create report object
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            TopGainers = results.OrderBy(r => r.Rank).Select(r => new
            {
                r.Rank,
                r.Symbol,
                PriceChangePercent = r.PriceChangePercent,
                AveragePrice = r.AveragePrice,
                LastPrice = r.LastPrice,
                Volume = r.Volume
            })
        };

        // Serialize to JSON with pretty formatting
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(report, options);
        
        // Write to file
        await File.WriteAllTextAsync(filepath, json, cancellationToken);

        _logger.LogInformation("Results saved to: {FilePath}", filepath);
        Console.WriteLine($"✅ Results saved to: {filepath}");
    }
}