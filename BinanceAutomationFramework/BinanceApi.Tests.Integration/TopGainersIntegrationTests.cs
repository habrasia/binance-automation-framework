using BinanceApi.Reporter;
using BinanceApi.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinanceApi.Tests.Integration;

/// <summary>
/// Integration tests that call the real Binance API.
/// These tests verify end-to-end functionality.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TopGainersIntegrationTests
{
    private ServiceProvider _serviceProvider = null!;
    private ILogger<TopGainersIntegrationTests> _logger = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // Build service provider once for all tests
        _serviceProvider = TestConfiguration.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<TopGainersIntegrationTests>>();
        
        // Validate configuration
        var config = TestConfiguration.BuildConfiguration();
        TestConfiguration.ValidateConfiguration(config);
        
        _logger.LogInformation("Integration tests initialized");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _serviceProvider?.Dispose();
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_ShouldReturnThreeResults()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act
        var results = await service.GetTopGainersAsync(count: 3);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3, "we requested top 3 gainers");
        
        _logger.LogInformation(
            "Retrieved top 3 gainers: {Symbols}", 
            string.Join(", ", results.Select(r => r.Symbol)));
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_ResultsShouldBeOrderedByRank()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act
        var results = await service.GetTopGainersAsync(count: 3);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeInAscendingOrder(r => r.Rank);
        results.First().Rank.Should().Be(1, "first result should be rank 1");
        results.Last().Rank.Should().Be(3, "last result should be rank 3");
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_ResultsShouldHaveValidData()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act
        var results = await service.GetTopGainersAsync(count: 3);

        // Assert
        results.Should().NotBeNull();
        
        foreach (var result in results)
        {
            result.Symbol.Should().NotBeNullOrWhiteSpace("each result should have a symbol");
            result.PriceChangePercent.Should().BeGreaterThan(0, 
                "top gainers should have positive price change");
            result.Rank.Should().BeInRange(1, 3, "ranks should be 1-3");
            
            // Average price might be 0 if API call failed (we have fallback logic)
            // But at least one of these prices should be > 0
            (result.AveragePrice > 0 || result.LastPrice > 0).Should().BeTrue(
                "at least one price should be available");
        }
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_WithConsoleReporter_ShouldOutputToConsole()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();
        var reporter = _serviceProvider.GetRequiredService<ConsoleReporter>();

        // Act
        var results = await service.GetTopGainersAsync(count: 3);
        
        // Capture console output (just verify no exceptions)
        await reporter.ReportAsync(results);

        // Assert
        results.Should().NotBeNull();
        // If we got here without exceptions, the reporter worked
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_WithJsonReporter_ShouldCreateFile()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();
        var reporter = _serviceProvider.GetRequiredService<JsonReporter>();

        // Act
        var results = await service.GetTopGainersAsync(count: 3);
        await reporter.ReportAsync(results);

        // Assert
        results.Should().NotBeNull();
        
        // Verify results directory was created
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "results");
        Directory.Exists(resultsDir).Should().BeTrue("results directory should be created");
        
        // Verify at least one JSON file exists
        var jsonFiles = Directory.GetFiles(resultsDir, "top_gainers_*.json");
        jsonFiles.Should().NotBeEmpty("at least one JSON file should be created");
        
        _logger.LogInformation("JSON report created: {Files}", jsonFiles.Length);
    }

    [Test]
    [Category("API")]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(10)]
    public async Task GetTopGainersAsync_WithDifferentCounts_ShouldReturnCorrectNumber(int count)
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act
        var results = await service.GetTopGainersAsync(count);

        // Assert
        results.Should().HaveCount(count, $"we requested top {count} gainers");
        results.Should().BeInAscendingOrder(r => r.Rank);
    }

    [Test]
    [Category("Validation")]
    public void GetTopGainersAsync_WithZeroCount_ShouldThrowArgumentException()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act & Assert
        var act = () => service.GetTopGainersAsync(count: 0);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Count must be greater than zero*");
    }

    [Test]
    [Category("Validation")]
    public void GetTopGainersAsync_WithNegativeCount_ShouldThrowArgumentException()
    {
        // Arrange
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();

        // Act & Assert
        var act = () => service.GetTopGainersAsync(count: -1);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Count must be greater than zero*");
    }
}