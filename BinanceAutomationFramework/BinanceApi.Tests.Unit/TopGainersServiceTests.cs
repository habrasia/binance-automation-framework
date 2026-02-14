using BinanceApi.Client;
using BinanceApi.Client.Models;
using BinanceApi.Services;
using BinanceApi.Services.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BinanceApi.Tests.Unit;

/// <summary>
/// Unit tests for TopGainersService using mocked dependencies.
/// These tests are fast, isolated, and deterministic.
/// </summary>
[TestFixture]
[Category("Unit")]
public class TopGainersServiceTests
{
    private Mock<IBinanceApiClient> _mockApiClient = null!;
    private Mock<ILogger<TopGainersService>> _mockLogger = null!;
    private TopGainersService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockApiClient = new Mock<IBinanceApiClient>();
        _mockLogger = new Mock<ILogger<TopGainersService>>();
        _service = new TopGainersService(_mockApiClient.Object, _mockLogger.Object);
    }

    [Test]
    public async Task GetTopGainersAsync_WithValidData_ReturnsTopThreeGainers()
    {
        // Arrange
        var mockTickers = CreateMockTickers();
        var mockAvgPrice = new AveragePriceData { Price = "50000.00" };

        _mockApiClient
            .Setup(x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTickers);

        _mockApiClient
            .Setup(x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAvgPrice);

        // Act
        var results = await _service.GetTopGainersAsync(count: 3);

        // Assert
        results.Should().HaveCount(3);
        results.Should().BeInAscendingOrder(r => r.Rank);
        
        // Verify top gainer is the one with highest percentage
        results.First().Symbol.Should().Be("SYMBOL1");
        results.First().PriceChangePercent.Should().Be(10.5m);
        results.First().Rank.Should().Be(1);
    }

    [Test]
    public async Task GetTopGainersAsync_CallsApiClientCorrectly()
    {
        // Arrange
        var mockTickers = CreateMockTickers();
        var mockAvgPrice = new AveragePriceData { Price = "50000.00" };

        _mockApiClient
            .Setup(x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTickers);

        _mockApiClient
            .Setup(x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAvgPrice);

        // Act
        await _service.GetTopGainersAsync(count: 3);

        // Assert
        _mockApiClient.Verify(
            x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()), 
            Times.Once, 
            "should fetch all tickers once");

        _mockApiClient.Verify(
            x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Exactly(3), 
            "should fetch average price for top 3 symbols");
    }

    [Test]
    public async Task GetTopGainersAsync_FiltersOutInvalidTickers()
    {
        // Arrange
        var mockTickers = new List<TickerData>
        {
            new() { Symbol = "VALID1", PriceChangePercent = "5.0", LastPrice = "100" },
            new() { Symbol = "", PriceChangePercent = "10.0", LastPrice = "200" }, // Invalid - empty symbol
            new() { Symbol = "VALID2", PriceChangePercent = "0", LastPrice = "300" }, // Invalid - 0% change
            new() { Symbol = "VALID3", PriceChangePercent = "3.0", LastPrice = "400" },
        };

        var mockAvgPrice = new AveragePriceData { Price = "50000.00" };

        _mockApiClient
            .Setup(x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTickers);

        _mockApiClient
            .Setup(x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAvgPrice);

        // Act
        var results = await _service.GetTopGainersAsync(count: 2);

        // Assert
        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Symbol));
        results.Should().NotContain(r => r.Symbol == ""); // Empty symbol filtered out
    }

    [Test]
    public async Task GetTopGainersAsync_HandlesAvgPriceFailureGracefully()
    {
        // Arrange
        var mockTickers = CreateMockTickers();

        _mockApiClient
            .Setup(x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTickers);

        // Simulate avgPrice call failing
        _mockApiClient
            .Setup(x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        // Act
        var results = await _service.GetTopGainersAsync(count: 3);

        // Assert
        results.Should().HaveCount(3, "should still return results despite avgPrice failures");
        results.Should().OnlyContain(r => r.AveragePrice == 0, 
            "average prices should be 0 when API calls fail");
    }

    [Test]
    public async Task GetTopGainersAsync_ReturnsCorrectRankOrder()
    {
        // Arrange
        var mockTickers = new List<TickerData>
        {
            new() { Symbol = "LOW", PriceChangePercent = "1.0", LastPrice = "100", Volume = "1000" },
            new() { Symbol = "HIGH", PriceChangePercent = "10.0", LastPrice = "200", Volume = "2000" },
            new() { Symbol = "MID", PriceChangePercent = "5.0", LastPrice = "150", Volume = "1500" },
        };

        var mockAvgPrice = new AveragePriceData { Price = "50000.00" };

        _mockApiClient
            .Setup(x => x.GetAllTickersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTickers);

        _mockApiClient
            .Setup(x => x.GetAveragePriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAvgPrice);

        // Act
        var results = await _service.GetTopGainersAsync(count: 3);

        // Assert
        results[0].Symbol.Should().Be("HIGH");
        results[0].Rank.Should().Be(1);
        
        results[1].Symbol.Should().Be("MID");
        results[1].Rank.Should().Be(2);
        
        results[2].Symbol.Should().Be("LOW");
        results[2].Rank.Should().Be(3);
    }

    [Test]
    public void GetTopGainersAsync_WithZeroCount_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetTopGainersAsync(count: 0);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("count")
            .WithMessage("*must be greater than zero*");
    }

    [Test]
    public void GetTopGainersAsync_WithNegativeCount_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => _service.GetTopGainersAsync(count: -5);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("count");
    }

    [Test]
    public void Constructor_WithNullApiClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TopGainersService(null!, _mockLogger.Object);
        
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TopGainersService(_mockApiClient.Object, null!);
        
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    /// <summary>
    /// Creates mock ticker data for testing.
    /// </summary>
    private static List<TickerData> CreateMockTickers()
    {
        return new List<TickerData>
        {
            new()
            {
                Symbol = "SYMBOL1",
                PriceChangePercent = "10.5",
                LastPrice = "50000.00",
                Volume = "1000000"
            },
            new()
            {
                Symbol = "SYMBOL2",
                PriceChangePercent = "8.3",
                LastPrice = "45000.00",
                Volume = "800000"
            },
            new()
            {
                Symbol = "SYMBOL3",
                PriceChangePercent = "6.7",
                LastPrice = "40000.00",
                Volume = "600000"
            },
            new()
            {
                Symbol = "SYMBOL4",
                PriceChangePercent = "2.1",
                LastPrice = "30000.00",
                Volume = "400000"
            }
        };
    }
}