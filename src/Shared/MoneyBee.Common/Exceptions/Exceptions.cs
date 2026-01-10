namespace MoneyBee.Common.Exceptions;

public class MoneyBeeException : Exception
{
    public MoneyBeeException(string message) : base(message)
    {
    }

    public MoneyBeeException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

public class ValidationException : MoneyBeeException
{
    public List<string> Errors { get; set; }

    public ValidationException(string message, List<string>? errors = null) 
        : base(message)
    {
        Errors = errors ?? new List<string>();
    }
}

public class NotFoundException : MoneyBeeException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : MoneyBeeException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

public class BusinessRuleException : MoneyBeeException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

public class ExternalServiceException : MoneyBeeException
{
    public string ServiceName { get; set; }

    public ExternalServiceException(string serviceName, string message) 
        : base($"External service '{serviceName}' error: {message}")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException) 
        : base($"External service '{serviceName}' error: {message}", innerException)
    {
        ServiceName = serviceName;
    }
}
