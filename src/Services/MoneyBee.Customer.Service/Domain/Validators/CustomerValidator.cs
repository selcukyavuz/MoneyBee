using MoneyBee.Common.Enums;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Constants;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Validators;

/// <summary>
/// Domain validator for Customer validation and business rules
/// </summary>
public static class CustomerValidator
{
    public static Result ValidateCustomerForCreation(CustomerEntity customer)
    {
        if (!customer.IsAdult())
            return Result.Failure(ErrorMessages.Customer.MustBeAdult);

        if (customer.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(customer.TaxNumber))
            return Result.Failure(ErrorMessages.Customer.CorporateMustHaveTaxNumber);

        return Result.Success();
    }

    public static bool CanCustomerReceiveTransfer(CustomerEntity customer)
    {
        return customer.Status != CustomerStatus.Blocked;
    }

    public static bool CanCustomerSendTransfer(CustomerEntity customer)
    {
        return customer.Status == CustomerStatus.Active && customer.IsAdult();
    }

    public static Result ValidateCustomerUpdate(CustomerEntity customer, CustomerStatus newStatus)
    {
        if (customer.Status == CustomerStatus.Blocked && newStatus == CustomerStatus.Active)
        {
            if (!customer.KycVerified)
                return Result.Failure(ErrorMessages.Customer.CannotActivateBlockedWithoutKyc);
        }

        return Result.Success();
    }
}
