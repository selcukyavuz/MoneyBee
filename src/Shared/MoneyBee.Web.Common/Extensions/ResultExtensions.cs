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
            return Results.Ok(ApiResponse<object>.SuccessResponse(null));
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => Results.NotFound(ApiResponse<object>.ErrorResponse(result.Error!)),
            ResultErrorType.Validation => Results.BadRequest(ApiResponse<object>.ErrorResponse(result.Error!)),
            ResultErrorType.Unauthorized => Results.Unauthorized(),
            ResultErrorType.Forbidden => Results.StatusCode(403),
            ResultErrorType.Conflict => Results.Conflict(ApiResponse<object>.ErrorResponse(result.Error!)),
            _ => Results.BadRequest(ApiResponse<object>.ErrorResponse(result.Error!))
        };
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(ApiResponse<T>.SuccessResponse(result.Value));
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => Results.NotFound(ApiResponse<T>.ErrorResponse(result.Error!)),
            ResultErrorType.Validation => Results.BadRequest(ApiResponse<T>.ErrorResponse(result.Error!)),
            ResultErrorType.Unauthorized => Results.Unauthorized(),
            ResultErrorType.Forbidden => Results.StatusCode(403),
            ResultErrorType.Conflict => Results.Conflict(ApiResponse<T>.ErrorResponse(result.Error!)),
            _ => Results.BadRequest(ApiResponse<T>.ErrorResponse(result.Error!))
        };
    }
}
