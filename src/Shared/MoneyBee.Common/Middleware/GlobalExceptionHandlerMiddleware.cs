using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MoneyBee.Common.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public partial class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions ProductionOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions DevelopmentOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse
                {
                    Error = "Internal server error",
                    Message = environment.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected error occurred. Please try again later.",
                    TraceId = context.TraceIdentifier,
                    Details = environment.IsDevelopment() ? exception.StackTrace : null
                }
            )
        };

        // Log exception with appropriate level
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, 
                "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}", 
                context.TraceIdentifier, 
                context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception,
                "Business exception occurred: {ExceptionType}. TraceId: {TraceId}, Path: {Path}",
                exception.GetType().Name,
                context.TraceIdentifier,
                context.Request.Path);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = environment.IsDevelopment() ? DevelopmentOptions : ProductionOptions;

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
