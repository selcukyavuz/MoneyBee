using FluentAssertions;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.UnitTests.Helpers;

public class NationalIdValidatorTests
{
    [Fact]
    public void IsValid_With10000000146_ShouldReturnTrue()
    {
        // Arrange - This is a valid TC based on algorithm:
        // Sum of first 10: 1+0+0+0+0+0+0+0+1+4 = 6, 6%10 = 6 (matches 11th digit)
        // Odd sum (1,3,5,7,9): 1+0+0+0+1 = 2
        // Even sum (2,4,6,8): 0+0+0+0 = 0
        // (2*7-0)%10 = 14%10 = 4 (matches 10th digit)
        var nationalId = "10000000146";

        // Act
        var isValid = NationalIdValidator.IsValid(nationalId);

        // Assert
        isValid.Should().BeTrue($"because {nationalId} should be valid");
    }
    
    [Fact]
    public void IsValid_WithAlgorithmicallyCorrectId_ShouldReturnTrue()
    {
        // Let's create a definitely valid ID step by step
        // Pattern: 1 2 3 4 5 6 7 8 X Y Z where X,Y,Z are calculated
        // Using simple pattern: 1 1 1 1 1 1 1 1 1 X Y
        // Odd positions (1,3,5,7,9): 1+1+1+1+1 = 5
        // Even positions (2,4,6,8): 1+1+1+1 = 4
        // X (10th digit) = (5*7-4)%10 = 31%10 = 1
        // First 10 sum: 1+1+1+1+1+1+1+1+1+1 = 10
        // Y (11th digit) = 10%10 = 0
        var nationalId = "11111111110";

        // Act
        var isValid = NationalIdValidator.IsValid(nationalId);

        // Assert
        isValid.Should().BeTrue($"because {nationalId} follows the algorithm correctly");
    }

    [Theory]
    [InlineData("00000000000")] // First digit cannot be 0
    [InlineData("12345678900")] // Invalid checksum
    [InlineData("1234567890")] // Too short
    [InlineData("123456789012")] // Too long
    [InlineData("1234567890a")] // Contains letter
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public void IsValid_WithInvalidNationalId_ShouldReturnFalse(string? nationalId)
    {
        // Act
        var isValid = NationalIdValidator.IsValid(nationalId!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("123 456 789 01", "12345678901")]
    [InlineData("123-456-789-01", "12345678901")]
    [InlineData("  12345678901  ", "12345678901")]
    public void Normalize_ShouldRemoveNonDigitCharacters(string input, string expected)
    {
        // Act
        var normalized = NationalIdValidator.Normalize(input);

        // Assert
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Normalize_WithEmptyOrWhitespace_ShouldReturnEmpty(string? input)
    {
        // Act
        var normalized = NationalIdValidator.Normalize(input!);

        // Assert
        normalized.Should().BeEmpty();
    }
}
