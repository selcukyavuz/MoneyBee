using FluentAssertions;
using MoneyBee.Auth.Service.Helpers;

namespace MoneyBee.Auth.Service.UnitTests.Helpers;

public class ApiKeyHelperTests
{
    [Fact]
    public void GenerateApiKey_ShouldReturn35CharacterKeyWithPrefix()
    {
        // Act
        var apiKey = ApiKeyHelper.GenerateApiKey();

        // Assert
        apiKey.Should().NotBeNullOrEmpty();
        apiKey.Should().HaveLength(35); // "mb_" (3) + 32 chars
        apiKey.Should().StartWith("mb_");
    }

    [Fact]
    public void GenerateApiKey_ShouldReturnUniqueKeys()
    {
        // Act
        var key1 = ApiKeyHelper.GenerateApiKey();
        var key2 = ApiKeyHelper.GenerateApiKey();
        var key3 = ApiKeyHelper.GenerateApiKey();

        // Assert
        key1.Should().NotBe(key2);
        key2.Should().NotBe(key3);
        key1.Should().NotBe(key3);
    }

    [Fact]
    public void GenerateApiKey_ShouldBeValidFormat()
    {
        // Act
        var apiKey = ApiKeyHelper.GenerateApiKey();

        // Assert
        ApiKeyHelper.IsValidApiKeyFormat(apiKey).Should().BeTrue();
    }

    [Fact]
    public void HashApiKey_WithValidKey_ShouldReturnBase64Hash()
    {
        // Arrange
        var apiKey = "test-api-key-12345";

        // Act
        var hashedKey = ApiKeyHelper.HashApiKey(apiKey);

        // Assert
        hashedKey.Should().NotBeNullOrEmpty();
        // SHA256 hash in Base64 format is 44 characters
        hashedKey.Should().HaveLength(44);
    }

    [Fact]
    public void HashApiKey_ShouldReturnDeterministicHash()
    {
        // Arrange
        var apiKey = "consistent-key";

        // Act
        var hash1 = ApiKeyHelper.HashApiKey(apiKey);
        var hash2 = ApiKeyHelper.HashApiKey(apiKey);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashApiKey_DifferentKeys_ShouldReturnDifferentHashes()
    {
        // Arrange
        var key1 = "key-one";
        var key2 = "key-two";

        // Act
        var hash1 = ApiKeyHelper.HashApiKey(key1);
        var hash2 = ApiKeyHelper.HashApiKey(key2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void MaskApiKey_WithValidKey_ShouldShowFirst3AndLast4Characters()
    {
        // Arrange - mb_ prefix (3 chars) + 32 chars = 35 total
        var apiKey = "mb_abcd1234567890wxyz1234567890wxyz"; // 35 chars

        // Act
        var maskedKey = ApiKeyHelper.MaskApiKey(apiKey);

        // Assert: "mb_" + "****...****" + "wxyz"
        maskedKey.Should().Be("mb_****...****wxyz");
    }

    [Fact]
    public void MaskApiKey_WithShortKey_ShouldReturnAsterisks()
    {
        // Arrange
        var apiKey = "short";

        // Act
        var maskedKey = ApiKeyHelper.MaskApiKey(apiKey);

        // Assert
        maskedKey.Should().Be("****");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MaskApiKey_WithEmptyKey_ShouldReturnAsterisks(string emptyKey)
    {
        // Act
        var maskedKey = ApiKeyHelper.MaskApiKey(emptyKey);

        // Assert
        maskedKey.Should().Be("****");
    }

    [Fact]
    public void IsValidApiKeyFormat_WithValidKey_ShouldReturnTrue()
    {
        // Arrange - Valid key: "mb_" (3 chars) + 32 chars = 35 total
        var validKey = "mb_abcdefghijklmnopqrstuvwxyz123456"; // exactly 35 chars

        // Act
        var isValid = ApiKeyHelper.IsValidApiKeyFormat(validKey);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("mb_short")]
    [InlineData("wrong_prefix_but_long_enough_string")]
    public void IsValidApiKeyFormat_WithInvalidKey_ShouldReturnFalse(string invalidKey)
    {
        // Act
        var isValid = ApiKeyHelper.IsValidApiKeyFormat(invalidKey);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateApiKey_MultipleTimes_ShouldProduceDifferentKeys()
    {
        // Arrange
        var keys = new HashSet<string>();
        var iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            keys.Add(ApiKeyHelper.GenerateApiKey());
        }

        // Assert
        keys.Should().HaveCount(iterations, "all generated keys should be unique");
    }

    [Fact]
    public void HashApiKey_ShouldProduceDeterministicResults()
    {
        // Arrange
        var apiKey = "deterministic-test-key";
        var iterations = 10;
        var hashes = new List<string>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            hashes.Add(ApiKeyHelper.HashApiKey(apiKey));
        }

        // Assert
        hashes.Distinct().Should().HaveCount(1, "same input should always produce same hash");
    }
}
