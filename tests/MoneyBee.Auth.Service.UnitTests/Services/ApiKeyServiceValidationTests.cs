using FluentAssertions;
using Microsoft.Extensions.Logging;
using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Services;
using MoneyBee.Auth.Service.Domain.ApiKeys;
using MoneyBee.Common.Results;
using Moq;

namespace MoneyBee.Auth.Service.UnitTests.Services;

public class ApiKeyServiceValidationTests
{
    private readonly Mock<IApiKeyRepository> _mockRepository;
    private readonly Mock<ILogger<ApiKeyService>> _mockLogger;
    private readonly ApiKeyService _service;

    public ApiKeyServiceValidationTests()
    {
        _mockRepository = new Mock<IApiKeyRepository>();
        _mockLogger = new Mock<ILogger<ApiKeyService>>();
        _service = new ApiKeyService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithEmptyName_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = "",
            Description = "Test"
        };

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("Name is required");
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithNameTooLong_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = new string('A', 101), // 101 characters
            Description = "Test"
        };

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("Name cannot exceed 100 characters");
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithDescriptionTooLong_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = "Test Key",
            Description = new string('A', 501) // 501 characters
        };

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("Description cannot exceed 500 characters");
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithInvalidExpiresInDays_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = "Test Key",
            ExpiresInDays = 0
        };

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("ExpiresInDays must be greater than 0");
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithExpiresInDaysTooLarge_ShouldReturnValidationError()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = "Test Key",
            ExpiresInDays = 3651 // More than 10 years
        };

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be("ExpiresInDays cannot exceed 3650 (10 years)");
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new CreateApiKeyRequest
        {
            Name = "Test Key",
            Description = "Test Description",
            ExpiresInDays = 30
        };

        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<Domain.ApiKeys.ApiKey>()))
            .ReturnsAsync((Domain.ApiKeys.ApiKey key) => key);

        // Act
        var result = await _service.CreateApiKeyAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Key");
        result.Value.ApiKey.Should().NotBeNullOrEmpty();
    }
}
