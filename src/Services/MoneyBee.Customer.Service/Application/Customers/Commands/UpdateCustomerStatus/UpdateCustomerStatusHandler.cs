using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Abstractions;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Domain.Services;

namespace MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomerStatus;

/// <summary>
/// Handles customer status updates
/// </summary>
public class UpdateCustomerStatusHandler(
    ICustomerRepository repository,
    IEventPublisher eventPublisher,
    ILogger<UpdateCustomerStatusHandler> logger) : ICommandHandler<UpdateCustomerStatusRequest, Result>
{
    public async Task<Result> HandleAsync(UpdateCustomerStatusRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Get customer
        var customer = await repository.GetByIdAsync(request.Id);
        if (customer is null)
        {
            return Result.NotFound(ErrorMessages.NotFound);
        }

        // 2. Validate status change
        var validationResult = CustomerValidator.ValidateCustomerUpdate(customer, request.Status);
        if (!validationResult.IsSuccess)
        {
            return Result.Validation(validationResult.Error!);
        }

        // 3. Capture old status before update for event
        var oldStatus = customer.Status;

        // 4. Update status using aggregate method
        customer.UpdateStatus(request.Status);

        // 5. Save changes
        await repository.UpdateAsync(customer);

        logger.LogInformation("Customer status updated: {CustomerId} -> {NewStatus}. Reason: {Reason}", 
            request.Id, request.Status, request.Reason);

        // 6. Publish integration event
        await eventPublisher.PublishAsync(new CustomerStatusChangedEvent
        {
            CustomerId = customer.Id,
            PreviousStatus = oldStatus.ToString(),
            NewStatus = customer.Status.ToString(),
            Reason = request.Reason ?? string.Empty
        });

        return Result.Success();
    }
}
