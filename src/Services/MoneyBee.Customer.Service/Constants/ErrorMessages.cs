namespace MoneyBee.Customer.Service.Constants;

/// <summary>
/// Error message constants for Customer Service
/// </summary>
public static class ErrorMessages
{
    public static class Customer
    {
        public const string NotFound = "Customer not found";
        public const string AlreadyExists = "Customer with this National ID already exists";
        public const string MustBeAdult = "Customer must be at least 18 years old";
        public const string CorporateMustHaveTaxNumber = "Corporate customers must have a tax number";
        public const string CannotActivateBlockedWithoutKyc = "Cannot activate blocked customer without KYC verification";
    }
}
