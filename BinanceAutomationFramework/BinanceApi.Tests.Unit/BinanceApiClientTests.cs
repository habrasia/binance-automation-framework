using BinanceApi.Client;
using BinanceApi.Client.Models;
using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace BinanceApi.Tests.Unit;

/// <summary>
/// Unit tests for BinanceApiClient HTTP layer.
/// Tests JSON deserialization, error handling, and argument validation.
/// </summary>
[TestFixture]
[Category("Unit")]
public class BinanceApiClientTests
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private BinanceApiClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _client = new BinanceApiClient(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    #region GetAllTickersAsync Tests

    [Test]
    public async Task GetAllTickersAsync_ValidResponse_ReturnsDeserializedTickers()
    {
        // Arrange
        var expectedJson = @"[
            {
                ""symbol"": ""BTCUSDT"",
                ""priceChange"": ""100.50"",
                ""priceChangePercent"": ""2.5"",
                ""weightedAvgPrice"": ""40000.00"",
                ""lastPrice"": ""40100.50"",
                ""openPrice"": ""40000.00"",
                ""highPrice"": ""40200.00"",
                ""lowPrice"": ""39900.00"",
                ""volume"": ""1000000"",
                ""closeTime"": 1234567890
            },
            {
                ""symbol"": ""ETHUSDT"",
                ""priceChange"": ""50.25"",
                ""priceChangePercent"": ""3.0"",
                ""weightedAvgPrice"": ""3000.00"",
                ""lastPrice"": ""3050.25"",
                ""openPrice"": ""3000.00"",
                ""highPrice"": ""3100.00"",
                ""lowPrice"": ""2900.00"",
                ""volume"": ""500000"",
                ""closeTime"": 1234567890
            }
        ]";

        SetupHttpResponse(HttpStatusCode.OK, expectedJson);

        // Act
        var result = await _client.GetAllTickersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        result[0].Symbol.Should().Be("BTCUSDT");
        result[0].PriceChangePercent.Should().Be("2.5");
        result[0].PriceChangePercentValue.Should().Be(2.5m);
        result[0].LastPrice.Should().Be("40100.50");
        
        result[1].Symbol.Should().Be("ETHUSDT");
        result[1].PriceChangePercentValue.Should().Be(3.0m);
    }

    [Test]
    public async Task GetAllTickersAsync_EmptyArray_ReturnsEmptyList()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "[]");

        // Act
        var result = await _client.GetAllTickersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllTickersAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{ invalid json }");

        // Act
        var act = () => _client.GetAllTickersAsync();

        // Assert
        await act.Should().ThrowAsync<JsonException>();
    }

    [Test]
    public async Task GetAllTickersAsync_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var act = () => _client.GetAllTickersAsync();

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task GetAllTickersAsync_RateLimited_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.TooManyRequests, "Rate limit exceeded");

        // Act
        var act = () => _client.GetAllTickersAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task GetAllTickersAsync_PropertyNameCaseInsensitive_DeserializesCorrectly()
    {
        // Arrange - Test case insensitivity
        var jsonWithDifferentCase = @"[{
            ""SYMBOL"": ""BTCUSDT"",
            ""pricechange"": ""100.50"",
            ""PRICECHANGEPERCENT"": ""2.5"",
            ""lastprice"": ""40100.50"",
            ""volume"": ""1000000"",
            ""closetime"": 1234567890
        }]";

        SetupHttpResponse(HttpStatusCode.OK, jsonWithDifferentCase);

        // Act
        var result = await _client.GetAllTickersAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("BTCUSDT");
        result[0].PriceChangePercent.Should().Be("2.5");
    }

    [Test]
    public async Task GetAllTickersAsync_MissingOptionalFields_DeserializesWithDefaults()
    {
        // Arrange
        var minimalJson = @"[{
            ""symbol"": ""BTCUSDT"",
            ""priceChangePercent"": ""2.5"",
            ""lastPrice"": ""40100.50"",
            ""closeTime"": 1234567890
        }]";

        SetupHttpResponse(HttpStatusCode.OK, minimalJson);

        // Act
        var result = await _client.GetAllTickersAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Symbol.Should().Be("BTCUSDT");
        result[0].Volume.Should().Be(string.Empty); // Default value
    }

    #endregion

    #region GetAveragePriceAsync Tests

    [Test]
    public async Task GetAveragePriceAsync_ValidResponse_ReturnsDeserializedData()
    {
        // Arrange
        var expectedJson = @"{
            ""mins"": 5,
            ""price"": ""40000.12345678"",
            ""closeTime"": 1234567890
        }";

        SetupHttpResponse(HttpStatusCode.OK, expectedJson);

        // Act
        var result = await _client.GetAveragePriceAsync("BTCUSDT");

        // Assert
        result.Should().NotBeNull();
        result.Mins.Should().Be(5);
        result.Price.Should().Be("40000.12345678");
        result.PriceValue.Should().Be(40000.12345678m);
        result.CloseTime.Should().Be(1234567890);
    }

    [Test]
    public async Task GetAveragePriceAsync_NullSymbol_ThrowsArgumentException()
    {
        // Act
        var act = () => _client.GetAveragePriceAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("symbol");
    }

    [Test]
    public async Task GetAveragePriceAsync_EmptySymbol_ThrowsArgumentException()
    {
        // Act
        var act = () => _client.GetAveragePriceAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("symbol");
    }

    [Test]
    public async Task GetAveragePriceAsync_WhitespaceSymbol_ThrowsArgumentException()
    {
        // Act
        var act = () => _client.GetAveragePriceAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("symbol");
    }

    [Test]
    public async Task GetAveragePriceAsync_NullResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "null");

        // Act
        var act = () => _client.GetAveragePriceAsync("BTCUSDT");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Null response*");
    }

    [Test]
    public async Task GetAveragePriceAsync_InvalidPriceFormat_ReturnsPriceValueZero()
    {
        // Arrange
        var jsonWithInvalidPrice = @"{
            ""mins"": 5,
            ""price"": ""not-a-number"",
            ""closeTime"": 1234567890
        }";

        SetupHttpResponse(HttpStatusCode.OK, jsonWithInvalidPrice);

        // Act
        var result = await _client.GetAveragePriceAsync("BTCUSDT");

        // Assert
        result.Price.Should().Be("not-a-number");
        result.PriceValue.Should().Be(0m, "invalid price strings should parse to 0");
    }

    [Test]
    public async Task GetAveragePriceAsync_NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, "Symbol not found");

        // Act
        var act = () => _client.GetAveragePriceAsync("INVALID");

        // Assert
        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetAveragePriceAsync_CorrectUrlFormat_IncludesSymbolParameter()
    {
        // Arrange
        var expectedJson = @"{""mins"": 5, ""price"": ""40000.00"", ""closeTime"": 1234567890}";
        
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedJson)
                };
            });

        // Act
        await _client.GetAveragePriceAsync("ETHBTC");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("symbol=ETHBTC");
        capturedRequest.RequestUri.ToString().Should().Contain("/avgPrice");
    }

    [Test]
    public async Task GetAveragePriceAsync_CancellationRequested_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var act = () => _client.GetAveragePriceAsync("BTCUSDT", cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion

    #region Constructor Tests

    [Test]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new BinanceApiClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var response = new HttpResponseMessage
        {
            StatusCode = statusCode,
            Content = new StringContent(content)
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #endregion
}