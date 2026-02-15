using BinanceApi.Reporter;
using BinanceApi.Services.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace BinanceApi.Tests.Unit;

/// <summary>
/// Unit tests for ConsoleReporter and JsonReporter.
/// Tests output formatting, file creation, and error handling.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ReporterTests
{
    #region ConsoleReporter Tests

    [Test]
    public async Task ConsoleReporter_WithValidResults_OutputsToConsole()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleReporter>>();
        var reporter = new ConsoleReporter(mockLogger.Object);
        var results = CreateSampleResults();

        // Capture console output
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            var output = consoleOutput.ToString();
            output.Should().Contain("TOP CRYPTO GAINERS");
            output.Should().Contain("RANK #1: BTCUSDT");
            output.Should().Contain("RANK #2: ETHUSDT");
            output.Should().Contain("RANK #3: ADAUSDT");
            output.Should().Contain("+10.50%");
            output.Should().Contain("+8.30%");
            output.Should().Contain("+5.20%");
            output.Should().Contain("Report generated at:");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task ConsoleReporter_WithEmptyResults_OutputsNoResultsMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleReporter>>();
        var reporter = new ConsoleReporter(mockLogger.Object);

        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        try
        {
            // Act
            await reporter.ReportAsync(new List<TopGainerResult>());

            // Assert
            var output = consoleOutput.ToString();
            output.Should().Contain("No results to report");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task ConsoleReporter_WithNullResults_OutputsNoResultsMessage()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleReporter>>();
        var reporter = new ConsoleReporter(mockLogger.Object);

        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);

        try
        {
            // Act
            await reporter.ReportAsync(null!);

            // Assert
            var output = consoleOutput.ToString();
            output.Should().Contain("No results to report");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public async Task ConsoleReporter_LogsReportedCount()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ConsoleReporter>>();
        var reporter = new ConsoleReporter(mockLogger.Object);
        var results = CreateSampleResults();

        // Redirect console to avoid test output pollution
        var originalOut = Console.Out;
        Console.SetOut(TextWriter.Null);

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("3") && v.ToString()!.Contains("top gainers")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void ConsoleReporter_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ConsoleReporter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region JsonReporter Tests

    [Test]
    public async Task JsonReporter_WithValidResults_CreatesJsonFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);
        var results = CreateSampleResults();

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            Directory.Exists(tempDir).Should().BeTrue("output directory should be created");
            
            var jsonFiles = Directory.GetFiles(tempDir, "top_gainers_*.json");
            jsonFiles.Should().NotBeEmpty("JSON file should be created");

            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            jsonContent.Should().NotBeNullOrWhiteSpace();

            // Verify JSON structure
            var jsonDoc = JsonDocument.Parse(jsonContent);
            jsonDoc.RootElement.GetProperty("generatedAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            
            var topGainers = jsonDoc.RootElement.GetProperty("topGainers");
            topGainers.GetArrayLength().Should().Be(3);
            
            var firstGainer = topGainers[0];
            firstGainer.GetProperty("rank").GetInt32().Should().Be(1);
            firstGainer.GetProperty("symbol").GetString().Should().Be("BTCUSDT");
            firstGainer.GetProperty("priceChangePercent").GetDecimal().Should().Be(10.50m);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_WithEmptyResults_LogsWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);

        try
        {
            // Act
            await reporter.ReportAsync(new List<TopGainerResult>());

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No results")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_WithNullResults_LogsWarning()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);

        try
        {
            // Act
            await reporter.ReportAsync(null!);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No results")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_DefaultDirectory_UsesResultsFolder()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var reporter = new JsonReporter(mockLogger.Object);
        var results = CreateSampleResults();

        var expectedDir = Path.Combine(Directory.GetCurrentDirectory(), "results");

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            Directory.Exists(expectedDir).Should().BeTrue();
            var jsonFiles = Directory.GetFiles(expectedDir, "top_gainers_*.json");
            jsonFiles.Should().NotBeEmpty();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(expectedDir))
            {
                var files = Directory.GetFiles(expectedDir, "top_gainers_*.json");
                foreach (var file in files)
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
    }

    [Test]
    public async Task JsonReporter_FileNameContainsTimestamp()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);
        var results = CreateSampleResults();

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            var jsonFiles = Directory.GetFiles(tempDir, "top_gainers_*.json");
            jsonFiles.Should().ContainSingle();
            
            var fileName = Path.GetFileNameWithoutExtension(jsonFiles[0]);
            fileName.Should().MatchRegex(@"top_gainers_\d{8}_\d{6}",
                "filename should contain YYYYMMDD_HHMMSS timestamp");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_JsonFormatting_IsPrettyPrinted()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);
        var results = CreateSampleResults();

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            var jsonFiles = Directory.GetFiles(tempDir, "top_gainers_*.json");
            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            
            jsonContent.Should().Contain("\n", "JSON should be indented");
            jsonContent.Should().Contain("  ", "JSON should use indentation");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_CamelCasePropertyNames()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);
        var results = CreateSampleResults();

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            var jsonFiles = Directory.GetFiles(tempDir, "top_gainers_*.json");
            var jsonContent = await File.ReadAllTextAsync(jsonFiles[0]);
            
            jsonContent.Should().Contain("\"generatedAt\"");
            jsonContent.Should().Contain("\"topGainers\"");
            jsonContent.Should().Contain("\"priceChangePercent\"");
            jsonContent.Should().NotContain("\"GeneratedAt\"");
            jsonContent.Should().NotContain("\"TopGainers\"");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task JsonReporter_LogsFilePath()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<JsonReporter>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var reporter = new JsonReporter(mockLogger.Object, tempDir);
        var results = CreateSampleResults();

        try
        {
            // Act
            await reporter.ReportAsync(results);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Results saved to")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void JsonReporter_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new JsonReporter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helper Methods

    private static List<TopGainerResult> CreateSampleResults()
    {
        return new List<TopGainerResult>
        {
            new()
            {
                Rank = 1,
                Symbol = "BTCUSDT",
                PriceChangePercent = 10.50m,
                AveragePrice = 40000.12345678m,
                LastPrice = 40100.50m,
                Volume = "1000000"
            },
            new()
            {
                Rank = 2,
                Symbol = "ETHUSDT",
                PriceChangePercent = 8.30m,
                AveragePrice = 3000.00m,
                LastPrice = 3050.25m,
                Volume = "500000"
            },
            new()
            {
                Rank = 3,
                Symbol = "ADAUSDT",
                PriceChangePercent = 5.20m,
                AveragePrice = 0.50m,
                LastPrice = 0.52m,
                Volume = "250000"
            }
        };
    }

    #endregion
}