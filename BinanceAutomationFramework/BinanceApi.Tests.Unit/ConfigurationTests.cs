using BinanceApi.Client;
using BinanceApi.Client.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BinanceApi.Tests.Unit;

/// <summary>
/// Unit tests for configuration validation and dependency injection setup.
/// Tests fail-fast validation at startup.
/// </summary>
[TestFixture]
[Category("Unit")]
public class ConfigurationTests
{
    #region BinanceApiOptions Validation Tests

    [Test]
    public void AddBinanceApiClient_ValidConfiguration_RegistersServicesSuccessfully()
    {
        // Arrange
        var configuration = CreateValidConfiguration();
        var services = new ServiceCollection();

        // Act
        services.AddBinanceApiClient(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IBinanceApiClient>();
        client.Should().NotBeNull("IBinanceApiClient should be registered");
        client.Should().BeOfType<BinanceApiClient>();
    }

    [Test]
    public void AddBinanceApiClient_MissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build(); // Empty config
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Configuration section*not found*");
    }

    [Test]
    public void AddBinanceApiClient_MissingApiKey_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWithout("ApiKey");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ApiKey is required*")
            .WithParameterName("ApiKey");
    }

    [Test]
    public void AddBinanceApiClient_EmptyApiKey_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWith("ApiKey", "");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ApiKey is required*");
    }

    [Test]
    public void AddBinanceApiClient_MissingBaseUrl_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWithout("BaseUrl");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*BaseUrl is required*")
            .WithParameterName("BaseUrl");
    }

    [Test]
    public void AddBinanceApiClient_InvalidBaseUrl_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWith("BaseUrl", "not-a-valid-url");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*not a valid absolute URI*");
    }

    [Test]
    public void AddBinanceApiClient_MissingApiHost_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWithout("ApiHost");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ApiHost is required*")
            .WithParameterName("ApiHost");
    }

    [Test]
    public void AddBinanceApiClient_ZeroTimeoutSeconds_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWith("TimeoutSeconds", "0");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TimeoutSeconds must be greater than zero*");
    }

    [Test]
    public void AddBinanceApiClient_NegativeTimeoutSeconds_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWith("TimeoutSeconds", "-10");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TimeoutSeconds must be greater than zero*");
    }

    [Test]
    public void AddBinanceApiClient_NegativeRetryCount_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfigurationWith("RetryCount", "-1");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*RetryCount cannot be negative*");
    }

    [Test]
    public void AddBinanceApiClient_ZeroRetryCount_DoesNotThrow()
    {
        // Arrange - Zero retry count is valid (no retries)
        var configuration = CreateConfigurationWith("RetryCount", "0");
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddBinanceApiClient(configuration);

        // Assert
        act.Should().NotThrow("zero retry count is valid");
    }

    [Test]
    public void BinanceApiOptions_SectionName_IsCorrect()
    {
        // Assert
        BinanceApiOptions.SectionName.Should().Be("BinanceApi");
    }

    [Test]
    public void BinanceApiOptions_DefaultValues_AreSet()
    {
        // Arrange
        var options = new BinanceApiOptions();

        // Assert
        options.BaseUrl.Should().Be(string.Empty);
        options.ApiKey.Should().Be(string.Empty);
        options.ApiHost.Should().Be(string.Empty);
        options.TimeoutSeconds.Should().Be(30);
        options.RetryCount.Should().Be(3);
    }

    #endregion

    #region Helper Methods

    private static IConfiguration CreateValidConfiguration()
    {
        var configData = new Dictionary<string, string>
        {
            {"BinanceApi:BaseUrl", "https://api.example.com"},
            {"BinanceApi:ApiKey", "test-api-key"},
            {"BinanceApi:ApiHost", "api.example.com"},
            {"BinanceApi:TimeoutSeconds", "30"},
            {"BinanceApi:RetryCount", "3"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    private static IConfiguration CreateConfigurationWithout(string excludedKey)
    {
        var configData = new Dictionary<string, string>
        {
            {"BinanceApi:BaseUrl", "https://api.example.com"},
            {"BinanceApi:ApiKey", "test-api-key"},
            {"BinanceApi:ApiHost", "api.example.com"},
            {"BinanceApi:TimeoutSeconds", "30"},
            {"BinanceApi:RetryCount", "3"}
        };

        configData.Remove($"BinanceApi:{excludedKey}");

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    private static IConfiguration CreateConfigurationWith(string key, string value)
    {
        var configData = new Dictionary<string, string>
        {
            {"BinanceApi:BaseUrl", "https://api.example.com"},
            {"BinanceApi:ApiKey", "test-api-key"},
            {"BinanceApi:ApiHost", "api.example.com"},
            {"BinanceApi:TimeoutSeconds", "30"},
            {"BinanceApi:RetryCount", "3"}
        };

        configData[$"BinanceApi:{key}"] = value;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    #endregion
}