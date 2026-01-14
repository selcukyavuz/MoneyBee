using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Domain.Customers;

/// <summary>
/// Domain Service for Customer validation and business rules
/// </summary>
public static class CustomerDomainService
{
    public static void ValidateCustomerForCreation(Customer customer)
    {
        if (!customer.IsAdult())
            throw new InvalidOperationException("Customer must be at least 18 years old");

        if (customer.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(customer.TaxNumber))
            throw new InvalidOperationException("Corporate customers must have a tax number");
    }

    public static bool CanCustomerReceiveTransfer(Customer customer)
    {
        return customer.Status != CustomerStatus.Blocked;
    }

    public static bool CanCustomerSendTransfer(Customer customer)
    {
        return customer.Status == CustomerStatus.Active && customer.IsAdult();
    }

    public static void ValidateCustomerUpdate(Customer customer, CustomerStatus newStatus)
    {
        if (customer.Status == CustomerStatus.Blocked && newStatus == CustomerStatus.Active)
        {
            if (!customer.KycVerified)
                throw new InvalidOperationException("Cannot activate blocked customer without KYC verification");
        }
    }
}
