using Microsoft.EntityFrameworkCore;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;

namespace MoneyBee.Customer.Service.BackgroundServices;

public class KycRetryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KycRetryService> _logger;
    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);

    public KycRetryService(
        IServiceProvider serviceProvider,
        ILogger<KycRetryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KYC Retry Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryUnverifiedCustomersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in KYC Retry Service");
            }

            await Task.Delay(_retryInterval, stoppingToken);
        }
    }

    private async Task RetryUnverifiedCustomersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var kycService = scope.ServiceProvider.GetRequiredService<IKycService>();

        // Get unverified customers created in last 24 hours
        var unverifiedCustomers = await repository.GetUnverifiedKycCustomersAsync(hours: 24);

        if (!unverifiedCustomers.Any())
        {
            _logger.LogDebug("No unverified customers to retry KYC");
            return;
        }

        _logger.LogInformation("Retrying KYC for {Count} customers", unverifiedCustomers.Count());

        foreach (var customer in unverifiedCustomers)
        {
            try
            {
                var result = await kycService.VerifyCustomerAsync(
                    customer.NationalId,
                    customer.FirstName,
                    customer.LastName,
                    customer.DateOfBirth);

                if (result.IsVerified)
                {
                    customer.KycVerified = true;
                    await repository.UpdateAsync(customer);
                    
                    _logger.LogInformation("KYC verification successful for customer {CustomerId}", customer.Id);
                }
                else
                {
                    _logger.LogWarning("KYC verification still failed for customer {CustomerId}: {Message}",
                        customer.Id, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying KYC for customer {CustomerId}", customer.Id);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("KYC Retry Service stopping");
        return base.StopAsync(cancellationToken);
    }
}
