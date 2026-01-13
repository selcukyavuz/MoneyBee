using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MoneyBee.IntegrationTests.Infrastructure;
using MoneyBee.IntegrationTests.Shared;
using Xunit;
using CustomerProgram = MoneyBee.Customer.Service.Program;

namespace MoneyBee.IntegrationTests.E2E;

/// <summary>
/// Integration tests for Customer Service
/// Tests single service with real database (Testcontainers)
/// External dependencies (KYC service) are mocked/stubbed
/// </summary>
[Collection("CustomerServiceTests")] // Run tests sequentially to avoid database conflicts
public class CustomerServiceTests : IClassFixture<IntegrationTestFactory<CustomerProgram>>
{
    private readonly HttpClient _client;

    public CustomerServiceTests(IntegrationTestFactory<CustomerProgram> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var request = new
        {
            firstName = "Ahmet",
            lastName = "Yılmaz",
            email = $"ahmet.yilmaz.{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905301234567",
            customerType = 0, // Individual
            nationalId = "15054682652", // Valid Turkish ID
            dateOfBirth = "1990-01-15",
            address = "Atatürk Caddesi No:1, Istanbul, Turkey 34000"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201, got {response.StatusCode}. Body: {errorBody}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            System.Text.Json.JsonSerializer.Serialize(result.Data),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer.Should().NotBeNull();
        customer!.Id.Should().NotBeEmpty();
        customer.FirstName.Should().Be("Ahmet");
        customer.LastName.Should().Be("Yılmaz");
        customer.Email.Should().Contain("ahmet.yilmaz");
        customer.Status.Should().Be("Active");
    }

    [Fact]
    public async Task GetCustomer_ById_ShouldReturnCorrectCustomer()
    {
        // Arrange - Create a customer first
        var customerId = await CreateTestCustomerAsync();

        // Act
        var response = await _client.GetAsync($"/api/customers/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            System.Text.Json.JsonSerializer.Serialize(result.Data),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer.Should().NotBeNull();
        customer!.Id.Should().Be(customerId);
    }

    [Fact]
    public async Task UpdateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var customerId = await CreateTestCustomerAsync();

        var updateRequest = new
        {
            firstName = "Mehmet",
            lastName = "Demir",
            email = $"mehmet.demir.{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905309876543"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/customers/{customerId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            System.Text.Json.JsonSerializer.Serialize(result.Data),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer.Should().NotBeNull();
        customer!.FirstName.Should().Be("Mehmet");
        customer.LastName.Should().Be("Demir");
    }

    [Fact]
    public async Task UpdateCustomerStatus_ToBlocked_ShouldSucceed()
    {
        // Arrange
        var customerId = await CreateTestCustomerAsync();

        var statusRequest = new
        {
            status = "Blocked",
            reason = "Suspicious activity detected"
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/customers/{customerId}/status", statusRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status changed
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            System.Text.Json.JsonSerializer.Serialize(result!.Data),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer!.Status.Should().Be("Blocked");
    }

    [Fact]
    public async Task DeleteCustomer_ShouldReturnNoContent()
    {
        // Arrange
        var customerId = await CreateTestCustomerAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/customers/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify customer is deleted
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllCustomers_ShouldReturnList()
    {
        // Arrange - Create multiple customers
        await CreateTestCustomerAsync();
        await CreateTestCustomerAsync();

        // Act
        var response = await _client.GetAsync("/api/customers?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            firstName = "Test",
            lastName = "User",
            email = "invalid-email", // Invalid email format
            phoneNumber = "+905301234567",
            nationalId = "10000000146",
            dateOfBirth = "1990-01-15",
            address = new
            {
                street = "Test Street",
                city = "Istanbul",
                country = "Turkey",
                postalCode = "34000"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/customers/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper Methods
    private static string GenerateValidTcKimlik(int seed)
    {
        // Generate a valid Turkish National ID based on user's provided example
        // Format: First 9 digits + calculated 10th digit + calculated 11th digit
        // Using variation based on seed to create multiple valid IDs
        // Modify digits 7, 8, 9 to create more variations (10,000 possible combinations)
        var baseDigits = new[] { 1, 5, 0, 5, 4, 6, 8, 2, 6 };
        
        // Modify last 3 digits (7th, 8th, 9th positions - 0-indexed 6,7,8)
        baseDigits[6] = (seed / 100) % 10;  // 7th digit: varies 0-9
        baseDigits[7] = (seed / 10) % 10;   // 8th digit: varies 0-9  
        baseDigits[8] = seed % 10;           // 9th digit: varies 0-9
        
        // Calculate 10th digit
        var oddSum = baseDigits[0] + baseDigits[2] + baseDigits[4] + baseDigits[6] + baseDigits[8];
        var evenSum = baseDigits[1] + baseDigits[3] + baseDigits[5] + baseDigits[7];
        var digit10 = (oddSum * 7 - evenSum) % 10;
        if (digit10 < 0) digit10 += 10;
        
        // Create array with 10 digits
        var allDigits = new int[11];
        Array.Copy(baseDigits, allDigits, 9);
        allDigits[9] = digit10;
        
        // Calculate 11th digit
        var sumFirst10 = allDigits.Take(10).Sum();
        allDigits[10] = sumFirst10 % 10;
        
        return string.Join("", allDigits.Select(d => d.ToString()));
    }
    
    private async Task<Guid> CreateTestCustomerAsync()
    {
        // Generate unique valid TC kimlik number with high entropy
        // Combine timestamp with random number and ensure seed is always different
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(100, 999);
        var seed = (int)((timestamp * random) % 10000);  // 0-9999 range for maximum variations
        var uniqueId = GenerateValidTcKimlik(seed);
        
        var request = new
        {
            firstName = $"Test{Random.Shared.Next(1000, 9999)}",
            lastName = $"User{Random.Shared.Next(1000, 9999)}",
            email = $"test{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = $"+90530{Random.Shared.Next(1000000, 9999999)}",
            customerType = 0, // Individual
            nationalId = uniqueId,
            dateOfBirth = "1990-01-15",
            address = "Test Street 123, Istanbul, Turkey 34000"
        };

        var response = await _client.PostAsJsonAsync("/api/customers", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"CreateTestCustomer failed: {response.StatusCode}. Body: {errorBody}");
        }
        
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            System.Text.Json.JsonSerializer.Serialize(result!.Data),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return customer!.Id;
    }
}

// Helper class for API response deserialization
public class ApiResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Message { get; set; }
    public object? Errors { get; set; }
}
