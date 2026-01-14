using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Constants;

namespace MoneyBee.Customer.Service.Domain.Customers;

/// <summary>
/// Domain validator for Customer validation and business rules
/// </summary>
public static class CustomerValidator
{
    public static Result ValidateCustomerForCreation(Customer customer)
    {
        if (!customer.IsAdult())
            return Result.Failure(ErrorMessages.Customer.MustBeAdult);

        if (customer.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(customer.TaxNumber))
            return Result.Validation(ErrorMessages.Customer.CorporateMustHaveTaxNumber);

        return Result.Success();
    }

    public static bool CanCustomerReceiveTransfer(Customer customer)
    {
        return customer.Status != CustomerStatus.Blocked;
    }

    public static bool CanCustomerSendTransfer(Customer customer)
    {
        return customer.Status == CustomerStatus.Active && customer.IsAdult();
    }

    public static Result ValidateCustomerUpdate(Customer customer, CustomerStatus newStatus)
    {
        if (customer.Status == CustomerStatus.Blocked && newStatus == CustomerStatus.Active)
        {
            if (!customer.KycVerified)
                return Result.Validation(ErrorMessages.Customer.CannotActivateBlockedWithoutKyc);
        }

        return Result.Success();
    }
}
