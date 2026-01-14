using MoneyBee.Common.Results;
using MoneyBee.Common.Serialization;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Infrastructure.Constants;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

public class CustomerService(
    HttpClient httpClient,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<Result<CustomerInfo>> GetCustomerByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        try
        {

            logger.LogInformation("Fetching customer by National ID: {NationalId}", nationalId);

            var response = await httpClient.GetAsync(string.Format(ExternalApiEndpoints.CustomerService.GetByNationalId, nationalId), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Customer Service returned error: {StatusCode}", response.StatusCode);
                return Result<CustomerInfo>.NotFound(TransferErrors.CustomerNotFound);
            }

            var result = await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonSerializerOptionsProvider.Default, cancellationToken);

            if (result?.Data == null)
            {
                logger.LogInformation("Customer not found: {NationalId}", nationalId);
                return Result<CustomerInfo>.NotFound(TransferErrors.CustomerNotFound);
            }

            return Result<CustomerInfo>.Success(result.Data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Customer Service");
            return Result<CustomerInfo>.Failure(TransferErrors.CustomerServiceUnavailable);
        }
    }
}
