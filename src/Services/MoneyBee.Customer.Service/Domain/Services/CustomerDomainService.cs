using MoneyBee.Common.Enums;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Services;

/// <summary>
/// Domain Service for Customer validation and business rules
/// </summary>
public class CustomerDomainService
{
    public void ValidateCustomerForCreation(CustomerEntity customer)
    {
        if (!customer.IsAdult())
            throw new InvalidOperationException("Customer must be at least 18 years old");

        if (customer.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(customer.TaxNumber))
            throw new InvalidOperationException("Corporate customers must have a tax number");
    }

    public bool CanCustomerReceiveTransfer(CustomerEntity customer)
    {
        return customer.Status != CustomerStatus.Blocked;
    }

    public bool CanCustomerSendTransfer(CustomerEntity customer)
    {
        return customer.Status == CustomerStatus.Active && customer.IsAdult();
    }

    public void ValidateCustomerUpdate(CustomerEntity customer, CustomerStatus newStatus)
    {
        if (customer.Status == CustomerStatus.Blocked && newStatus == CustomerStatus.Active)
        {
            if (!customer.KycVerified)
                throw new InvalidOperationException("Cannot activate blocked customer without KYC verification");
        }
    }
}
