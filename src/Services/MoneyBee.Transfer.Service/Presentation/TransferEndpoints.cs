namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Transfer endpoints registration
/// </summary>
public static class TransferEndpoints
{
    /// <summary>
    /// Maps all Transfer endpoints to the route builder
    /// </summary>
    public static RouteGroupBuilder MapTransferEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/transfers")
            .WithTags("Transfers")
            .WithOpenApi();

        group.MapCreateTransfer();
        group.MapCompleteTransfer();
        group.MapCancelTransfer();
        group.MapGetTransferByCode();
        group.MapGetCustomerTransfers();
        group.MapCheckDailyLimit();

        return group;
    }
}
