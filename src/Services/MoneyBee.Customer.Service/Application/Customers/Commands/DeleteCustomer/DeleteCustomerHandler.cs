using MoneyBee.Common.Events;
using MoneyBee.Common.Abstractions;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Commands.DeleteCustomer;

/// <summary>
/// Handles customer deletion
/// </summary>
public class DeleteCustomerHandler(
    ICustomerRepository repository,
    IEventPublisher eventPublisher,
    ILogger<DeleteCustomerHandler> logger) : ICommandHandler<Guid, bool>
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // 1. Get customer
        var customer = await repository.GetByIdAsync(id);
        if (customer is null)
        {
            return false;
        }

        // 2. Mark for deletion and raise domain event
        customer.MarkForDeletion();

        // 3. Delete
        var deleted = await repository.DeleteAsync(id);

        if (deleted)
        {
            logger.LogWarning("Customer deleted: {CustomerId}", id);

            // 4. Publish integration event
            await eventPublisher.PublishAsync(new CustomerDeletedEvent
            {
                CustomerId = customer.Id,
                NationalId = customer.NationalId,
                Timestamp = DateTime.UtcNow
            });
        }

        return deleted;
    }
}
