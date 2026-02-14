using BinanceApi.Reporter;
using BinanceApi.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BinanceApi.Tests.Integration;

/// <summary>
/// Integration tests that verify end-to-end functionality against real RapidAPI.
/// Tests skip gracefully if geo-blocked (HTTP 451).
/// Run locally to verify: dotnet test --filter "Category=Integration"
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
        _serviceProvider = TestConfiguration.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<TopGainersIntegrationTests>>();
        
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
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var results = await service.GetTopGainersAsync(count: 3);

            results.Should().NotBeNull();
            results.Should().HaveCount(3, "we requested top 3 gainers");
            
            _logger.LogInformation("✅ Test passed: {Symbols}", string.Join(", ", results.Select(r => r.Symbol)));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_ResultsShouldBeOrderedByRank()
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var results = await service.GetTopGainersAsync(count: 3);

            results.Should().NotBeNull();
            results.Should().BeInAscendingOrder(r => r.Rank);
            results.First().Rank.Should().Be(1);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_ResultsShouldHaveValidData()
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var results = await service.GetTopGainersAsync(count: 3);

            results.Should().NotBeNull();
            
            foreach (var result in results)
            {
                result.Symbol.Should().NotBeNullOrWhiteSpace();
                result.PriceChangePercent.Should().BeGreaterThan(0);
                result.Rank.Should().BeGreaterThan(0);
                (result.AveragePrice > 0 || result.LastPrice > 0).Should().BeTrue();
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_WithConsoleReporter_ShouldOutputToConsole()
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var reporter = _serviceProvider.GetRequiredService<ConsoleReporter>();

            var results = await service.GetTopGainersAsync(count: 3);
            await reporter.ReportAsync(results);

            results.Should().NotBeNull();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("API")]
    public async Task GetTopGainersAsync_WithJsonReporter_ShouldCreateFile()
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var reporter = _serviceProvider.GetRequiredService<JsonReporter>();

            var results = await service.GetTopGainersAsync(count: 3);
            await reporter.ReportAsync(results);

            results.Should().NotBeNull();
            
            var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "results");
            Directory.Exists(resultsDir).Should().BeTrue();
            
            var jsonFiles = Directory.GetFiles(resultsDir, "top_gainers_*.json");
            jsonFiles.Should().NotBeEmpty();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("API")]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(10)]
    public async Task GetTopGainersAsync_WithDifferentCounts_ShouldReturnCorrectNumber(int count)
    {
        try
        {
            var service = _serviceProvider.GetRequiredService<ITopGainersService>();
            var results = await service.GetTopGainersAsync(count);

            results.Should().HaveCount(count);
            results.Should().BeInAscendingOrder(r => r.Rank);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnavailableForLegalReasons)
        {
            Assert.Ignore("⚠️  Geo-blocked (HTTP 451). Run locally: dotnet test --filter \"Category=Integration\"");
        }
    }

    [Test]
    [Category("Validation")]
    public void GetTopGainersAsync_WithZeroCount_ShouldThrowArgumentException()
    {
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();
        var act = () => service.GetTopGainersAsync(count: 0);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Count must be greater than zero*");
    }

    [Test]
    [Category("Validation")]
    public void GetTopGainersAsync_WithNegativeCount_ShouldThrowArgumentException()
    {
        var service = _serviceProvider.GetRequiredService<ITopGainersService>();
        var act = () => service.GetTopGainersAsync(count: -1);
        
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Count must be greater than zero*");
    }
}