using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;

namespace MoneyBee.Transfer.Service.Endpoints;

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
        ITransferService transferService)
    {
        var limitInfo = await transferService.CheckDailyLimitAsync(customerId);
        return Results.Ok(ApiResponse<DailyLimitCheckResponse>.SuccessResponse(limitInfo));
    }
}
