using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MoneyBee.Auth.Service.Infrastructure.Caching;
using StackExchange.Redis;

namespace MoneyBee.Auth.Service.UnitTests.Infrastructure.Caching;

public class ApiKeyCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<ILogger<ApiKeyCacheService>> _loggerMock;
    private readonly ApiKeyCacheService _cacheService;

    public ApiKeyCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<ILogger<ApiKeyCacheService>>();

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);

        _cacheService = new ApiKeyCacheService(_redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetValidationResultAsync_WithCachedValue_ShouldReturnCachedResult()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        var cachedValue = "True";
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedValue));

        // Act
        var result = await _cacheService.GetValidationResultAsync(keyHash);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetValidationResultAsync_WithNoCachedValue_ShouldReturnNull()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetValidationResultAsync(keyHash);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetValidationResultAsync_WithRedisException_ShouldReturnNullAndLog()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.None, "Connection failed"));

        // Act
        var result = await _cacheService.GetValidationResultAsync(keyHash);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetValidationResultAsync_WithValidData_ShouldCacheResult()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        var isValid = true;
        var expiration = TimeSpan.FromMinutes(5);

        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetValidationResultAsync(keyHash, isValid, expiration);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(keyHash)),
            It.Is<RedisValue>(v => v == "True"),
            expiration,
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetValidationResultAsync_WithRedisException_ShouldNotThrow()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.None, "Connection failed"));

        // Act
        var act = () => _cacheService.SetValidationResultAsync(keyHash, true, TimeSpan.FromMinutes(5));

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldDeleteKey()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.InvalidateCacheAsync(keyHash);

        // Assert
        _databaseMock.Verify(x => x.KeyDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateCacheAsync_WithRedisException_ShouldNotThrow()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.None, "Connection failed"));

        // Act
        var act = () => _cacheService.InvalidateCacheAsync(keyHash);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public async Task GetValidationResultAsync_WithDifferentBoolValues_ShouldParseCorrectly(
        string cachedValue, bool expected)
    {
        // Arrange
        var keyHash = "test-hash-12345";
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedValue));

        // Act
        var result = await _cacheService.GetValidationResultAsync(keyHash);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task SetValidationResultAsync_WithFalseValue_ShouldCacheFalse()
    {
        // Arrange
        var keyHash = "test-hash-12345";
        var expiration = TimeSpan.FromMinutes(1);

        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.SetValidationResultAsync(keyHash, false, expiration);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            It.Is<RedisKey>(k => k.ToString().Contains(keyHash)),
            It.Is<RedisValue>(v => v == "False"),
            expiration,
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
