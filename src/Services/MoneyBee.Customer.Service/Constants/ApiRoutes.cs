namespace MoneyBee.Customer.Service.Constants;

/// <summary>
/// API route constants for Customer Service
/// </summary>
public static class ApiRoutes
{
    public const string BaseRoute = "/api/customers";
    
    public static class Customers
    {
        public const string GetById = "{id:guid}";
        public const string GetByNationalId = "by-national-id/{nationalId}";
        public const string GetAll = "";
        public const string Create = "";
        public const string Update = "{id:guid}";
        public const string Delete = "{id:guid}";
        public const string UpdateStatus = "{id:guid}/status";
    }
}
