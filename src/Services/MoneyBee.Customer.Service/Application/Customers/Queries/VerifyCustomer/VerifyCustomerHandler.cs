using MoneyBee.Common.Abstractions;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Domain.Services;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.VerifyCustomer;

/// <summary>
/// Handles customer verification for transfers
/// </summary>
public class VerifyCustomerHandler(
    ICustomerRepository repository) : IQueryHandler<string, CustomerVerificationResponse>
{
    public async Task<CustomerVerificationResponse> HandleAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await repository.GetByNationalIdAsync(nationalId);

            if (customer is null)
            {
                return new CustomerVerificationResponse
                {
                    Exists = false,
                    Message = "Customer not found"
                };
            }

            // Use domain service to check if customer can send transfers
            var canSend = CustomerValidator.CanCustomerSendTransfer(customer);

            if (!canSend)
            {
                return new CustomerVerificationResponse
                {
                    Exists = true,
                    CustomerId = customer.Id,
                    Status = customer.Status,
                    KycVerified = customer.KycVerified,
                    IsActive = false,
                    Message = $"Customer exists but status is {customer.Status}"
                };
            }

            return new CustomerVerificationResponse
            {
                Exists = true,
                CustomerId = customer.Id,
                Status = customer.Status,
                KycVerified = customer.KycVerified,
                IsActive = true,
                Message = "Customer found and active"
            };
        }
        catch (ArgumentException ex)
        {
            return new CustomerVerificationResponse
            {
                Exists = false,
                Message = ex.Message
            };
        }
    }
}
