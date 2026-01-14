using Microsoft.AspNetCore.Http;
using MoneyBee.Common.Models;
using MoneyBee.Common.Results;

namespace MoneyBee.Web.Common.Extensions;

/// <summary>
/// Shared extension methods for converting Result to IResult with appropriate HTTP status codes
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse<object?>.SuccessResponse(null));
        }

        return CreateErrorResult<object>(result.ErrorType, result.Error!);
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse<T>.SuccessResponse(result.Value!));
        }

        return CreateErrorResult<T>(result.ErrorType, result.Error!);
    }

    private static IResult CreateErrorResult<T>(ResultErrorType errorType, string error)
    {
        return errorType switch
        {
            ResultErrorType.NotFound => Results.NotFound(ApiResponse<T>.ErrorResponse(error)),
            ResultErrorType.Validation => Results.BadRequest(ApiResponse<T>.ErrorResponse(error)),
            ResultErrorType.Unauthorized => Results.Unauthorized(),
            ResultErrorType.Forbidden => Results.StatusCode(403),
            ResultErrorType.Conflict => Results.Conflict(ApiResponse<T>.ErrorResponse(error)),
            _ => Results.BadRequest(ApiResponse<T>.ErrorResponse(error))
        };
    }
}
