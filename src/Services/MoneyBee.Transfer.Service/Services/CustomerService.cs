using MoneyBee.Common.Enums;
using MoneyBee.Common.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoneyBee.Transfer.Service.Services;

public interface ICustomerService
{
    Task<CustomerInfo?> GetCustomerByNationalIdAsync(string nationalId);
}

public class CustomerInfo
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; }
    public bool KycVerified { get; set; }
}

public class CustomerService : ICustomerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomerService(
        IHttpClientFactory httpClientFactory,
        ILogger<CustomerService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("CustomerService");
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:CustomerService"] ?? "http://customer-service");
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<CustomerInfo?> GetCustomerByNationalIdAsync(string nationalId)
    {
        try
        {
            _logger.LogInformation("Fetching customer by National ID: {NationalId}", nationalId);

            var response = await _httpClient.GetAsync($"/api/customers/verify/{nationalId}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Customer Service returned error: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CustomerVerificationResponse>(_jsonOptions);

            if (result?.Data?.Exists != true)
            {
                _logger.LogInformation("Customer not found: {NationalId}", nationalId);
                return null;
            }

            // Get full customer details
            var customerResponse = await _httpClient.GetAsync($"/api/customers/{result.Data.CustomerId}");

            if (!customerResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get customer details: {StatusCode}", customerResponse.StatusCode);
                return null;
            }

            var customerResult = await customerResponse.Content.ReadFromJsonAsync<CustomerDetailsResponse>(_jsonOptions);

            if (customerResult?.Data == null)
            {
                return null;
            }

            return new CustomerInfo
            {
                Id = customerResult.Data.Id,
                FirstName = customerResult.Data.FirstName,
                LastName = customerResult.Data.LastName,
                NationalId = customerResult.Data.NationalId,
                Status = customerResult.Data.Status,
                KycVerified = customerResult.Data.KycVerified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Customer Service");
            throw new ExternalServiceException("CustomerService", "Error fetching customer", ex);
        }
    }

    private class CustomerVerificationResponse
    {
        public CustomerVerificationData? Data { get; set; }
    }

    private class CustomerVerificationData
    {
        public bool Exists { get; set; }
        public Guid? CustomerId { get; set; }
    }

    private class CustomerDetailsResponse
    {
        public CustomerInfo? Data { get; set; }
    }
}
