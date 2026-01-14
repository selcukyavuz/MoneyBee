using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.Transfers.Queries.CheckDailyLimit;

namespace MoneyBee.Transfer.Service.Presentation;

/// <summary>
/// Check daily limit endpoint
/// </summary>
public static class CheckDailyLimitEndpoint
{
    public static RouteGroupBuilder MapCheckDailyLimit(this RouteGroupBuilder group)
    {
        group.MapGet("/daily-limit/{customerId:guid}", HandleAsync)
            .WithName("CheckDailyLimit")
            .WithSummary("Check daily limit for customer")
            .Produces<ApiResponse<DailyLimitCheckResponse>>(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid customerId,
        CheckDailyLimitHandler handler)
    {
        var limitInfo = await handler.HandleAsync(customerId);
        return Results.Ok(ApiResponse<DailyLimitCheckResponse>.SuccessResponse(limitInfo));
    }
}
