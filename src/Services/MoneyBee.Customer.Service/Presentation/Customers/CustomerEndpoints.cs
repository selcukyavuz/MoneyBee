namespace MoneyBee.Customer.Service.Presentation.Customers;

/// <summary>
/// Customer endpoints registration
/// </summary>
public static class CustomerEndpoints
{
    /// <summary>
    /// Maps all Customer endpoints to the route builder
    /// </summary>
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(ApiRoutes.BaseRoute)
            .WithTags("Customers")
            .WithOpenApi();

        group.MapCreateCustomer();
        group.MapGetCustomer();
        group.MapGetCustomers();
        group.MapGetCustomerByNationalId();
        group.MapUpdateCustomer();
        group.MapUpdateCustomerStatus();
        group.MapDeleteCustomer();
        group.MapVerifyCustomer();

        return group;
    }
}
