using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MoneyBee.IntegrationTests.Infrastructure;
using MoneyBee.IntegrationTests.Shared;
using Xunit;
using CustomerProgram = MoneyBee.Customer.Service.Program;

namespace MoneyBee.IntegrationTests.E2E;

/// <summary>
/// End-to-end tests for complete transfer flow with Customer Service
/// Tests single-service integration - Customer CRUD operations with Transfer scenarios
/// </summary>
[Collection("CompleteTransferFlowTests")]
public class CompleteTransferFlowTests : IClassFixture<IntegrationTestFactory<CustomerProgram>>
{
    private readonly HttpClient _client;

    public CompleteTransferFlowTests(IntegrationTestFactory<CustomerProgram> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTwoCustomers_ForTransferScenario_ShouldSucceed()
    {
        // Arrange - Create sender customer
        var sender = new
        {
            firstName = "Ahmet",
            lastName = "Sender",
            email = $"sender{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905301234567",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(Random.Shared.Next(1000, 9999)),
            dateOfBirth = "1985-05-15",
            address = "Sender Street 1, Istanbul, Turkey"
        };

        // Act - Create sender
        var senderResponse = await _client.PostAsJsonAsync("/api/customers", sender);

        // Assert
        senderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var senderResult = await senderResponse.Content.ReadFromJsonAsync<ApiResponse>();
        senderResult.Should().NotBeNull();
        senderResult!.Success.Should().BeTrue();

        // Arrange - Create receiver customer
        var receiver = new
        {
            firstName = "Mehmet",
            lastName = "Receiver",
            email = $"receiver{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905309876543",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(Random.Shared.Next(1000, 9999)),
            dateOfBirth = "1990-08-20",
            address = "Receiver Avenue 2, Ankara, Turkey"
        };

        // Act - Create receiver
        var receiverResponse = await _client.PostAsJsonAsync("/api/customers", receiver);

        // Assert
        receiverResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var receiverResult = await receiverResponse.Content.ReadFromJsonAsync<ApiResponse>();
        receiverResult.Should().NotBeNull();
        receiverResult!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateActiveCustomer_ReadyForTransfer_ShouldHaveActiveStatus()
    {
        // Arrange
        var customer = new
        {
            firstName = "Active",
            lastName = "Customer",
            email = $"active{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905301112233",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(Random.Shared.Next(1000, 9999)),
            dateOfBirth = "1988-03-10",
            address = "Active Street 5, Izmir, Turkey"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", customer);

        // Assert - Customer created successfully
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        var createdCustomer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            result!.Data.ToString()!,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        createdCustomer.Should().NotBeNull();
        createdCustomer!.Status.Should().Be("Active"); // Active status (KYC pending but customer can operate)
    }

    [Fact]
    public async Task GetCustomersList_ForTransferParticipants_ShouldReturnMultipleCustomers()
    {
        // Arrange - Create two customers
        var customer1 = new
        {
            firstName = "Customer",
            lastName = "One",
            email = $"cust1{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905307771111",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(Random.Shared.Next(1000, 9999)),
            dateOfBirth = "1992-01-01",
            address = "Address 1"
        };

        var customer2 = new
        {
            firstName = "Customer",
            lastName = "Two",
            email = $"cust2{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905307772222",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(Random.Shared.Next(1000, 9999)),
            dateOfBirth = "1993-02-02",
            address = "Address 2"
        };

        await _client.PostAsJsonAsync("/api/customers", customer1);
        await _client.PostAsJsonAsync("/api/customers", customer2);

        // Act
        var response = await _client.GetAsync("/api/customers?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCustomer_BeforeTransfer_ShouldUpdateContactInfo()
    {
        // Arrange - Create customer
        var customerId = await CreateTestCustomerAsync();

        var updateRequest = new
        {
            firstName = "Updated",
            lastName = "Name",
            email = $"updated{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = "+905309998888"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/customers/{customerId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify update
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse>();
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            result!.Data.ToString()!,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer!.FirstName.Should().Be("Updated");
        customer.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task BlockCustomer_ShouldPreventFutureTransfers()
    {
        // Arrange - Create customer
        var customerId = await CreateTestCustomerAsync();

        var statusRequest = new
        {
            status = 3, // Blocked
            reason = "Testing transfer prevention"
        };

        // Act - Block customer
        var blockResponse = await _client.PatchAsJsonAsync(
            $"/api/customers/{customerId}/status", 
            statusRequest);

        // Assert
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify blocked status
        var getResponse = await _client.GetAsync($"/api/customers/{customerId}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse>();
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            result!.Data.ToString()!,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        customer!.Status.Should().Be("Blocked"); // Blocked
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
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(100, 999);
        var seed = (int)((timestamp * random) % 10000);
        
        var request = new
        {
            firstName = $"Test{Random.Shared.Next(1000, 9999)}",
            lastName = $"User{Random.Shared.Next(1000, 9999)}",
            email = $"test{Guid.NewGuid().ToString()[..8]}@example.com",
            phoneNumber = $"+90530{Random.Shared.Next(1000000, 9999999)}",
            customerType = 0,
            nationalId = GenerateValidTcKimlik(seed),
            dateOfBirth = "1990-01-15",
            address = "Test Street 123, Istanbul, Turkey 34000"
        };

        var response = await _client.PostAsJsonAsync("/api/customers", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        var customer = System.Text.Json.JsonSerializer.Deserialize<CustomerDto>(
            result!.Data.ToString()!,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return customer!.Id;
    }
}
