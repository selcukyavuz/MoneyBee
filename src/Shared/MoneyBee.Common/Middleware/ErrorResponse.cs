namespace MoneyBee.Common.Middleware;

public partial class GlobalExceptionHandlerMiddleware
{
    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
