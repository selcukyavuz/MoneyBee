using Microsoft.Extensions.Options;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Common.Services;
using MoneyBee.Transfer.Service.Application.Constants;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Transfers;
using MoneyBee.Transfer.Service.Application.Options;
using MoneyBee.Transfer.Service.Domain.Transfers;
using MoneyBee.Transfer.Service.Domain.Transfers;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;

namespace MoneyBee.Transfer.Service.Application.Transfers;

public class TransferValidationService(
    ITransferRepository repository,
    ICustomerService customerService,
    IExchangeRateService exchangeRateService,
    IDistributedLockService distributedLock,
    ILogger<TransferValidationService> logger,
    IOptions<TransferSettings> transferSettings,
    IOptions<DistributedLockSettings> lockSettings)
{
    private readonly TransferSettings _transferSettings = transferSettings.Value;
    private readonly DistributedLockSettings _lockSettings = lockSettings.Value;

    public async Task<Result<CreateTransferResponse>?> CheckIdempotencyAsync(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<CreateTransferResponse>.Validation(TransferErrors.IdempotencyKeyRequired);
        }

        var existingTransfer = await repository.GetByIdempotencyKeyAsync(idempotencyKey);
        if (existingTransfer is not null)
        {
            logger.LogInformation("Idempotent request detected: {IdempotencyKey}", idempotencyKey);
            return Result<CreateTransferResponse>.Success(TransferMapper.ToCreateResponse(existingTransfer));
        }

        return null;
    }

    public async Task<Result<CustomerInfo>> ValidateCustomerAsync(
        string nationalId, string errorNotFound, string errorNotActive, CancellationToken cancellationToken = default)
    {
        var customerResult = await customerService.GetCustomerByNationalIdAsync(nationalId, cancellationToken);
        if (!customerResult.IsSuccess || customerResult.Value is null)
        {
            return Result<CustomerInfo>.Failure(errorNotFound);
        }
        
        var customer = customerResult.Value;
        if (customer.Status != CustomerStatus.Active)
        {
            return Result<CustomerInfo>.Failure(errorNotActive);
        }

        return Result<CustomerInfo>.Success(customer);
    }

    public async Task<Result<TransferAmount>> CalculateAmountInTRYAsync(
        decimal amount, Currency currency)
    {
        if (currency == Currency.TRY)
        {
            return Result<TransferAmount>.Success(new TransferAmount(amount, null));
        }

        var rateResult = await exchangeRateService.GetExchangeRateAsync(currency, Currency.TRY);
        
        if (!rateResult.IsSuccess)
        {
            logger.LogError("Failed to get exchange rate for {Currency}: {Error}", currency, rateResult.Error);
            return Result<TransferAmount>.Failure(TransferErrors.ExchangeRateUnavailable);
        }

        var exchangeRate = rateResult.Value!.Rate;
        var amountInTRY = amount * exchangeRate;
        
        return Result<TransferAmount>.Success(new TransferAmount(amountInTRY, exchangeRate));
    }

    public async Task<Result<bool>> CheckDailyLimitWithLockAsync(Guid customerId, decimal amountInTRY)
    {
        try
        {
            await distributedLock.ExecuteWithLockAsync(
                lockKey: $"customer:{customerId}:daily-limit",
                expiry: TimeSpan.FromSeconds(_lockSettings.DailyLimitCheckTimeoutSeconds),
                async () =>
                {
                    var dailyTotal = await repository.GetDailyTotalAsync(customerId, DateTime.Today);
                    var result = TransferValidator.ValidateDailyLimit(dailyTotal, amountInTRY, _transferSettings.DailyLimitTRY);
                    
                    if (!result.IsSuccess)
                    {
                        throw new InvalidOperationException(result.Error);
                    }
                    
                    logger.LogDebug(
                        "Daily limit check passed for customer {CustomerId}: {DailyTotal} + {Amount} <= {Limit}",
                        customerId, dailyTotal, amountInTRY, _transferSettings.DailyLimitTRY);
                    
                    return true;
                });

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }
    }
}
