namespace MoneyBee.Common.Results;

/// <summary>
/// Defines the type of error that occurred in a Result operation
/// Maps to appropriate HTTP status codes when handled globally
/// </summary>
public enum ResultErrorType
{
    /// <summary>
    /// General failure - maps to 400 Bad Request
    /// </summary>
    Failure = 0,
    
    /// <summary>
    /// Resource not found - maps to 404 Not Found
    /// </summary>
    NotFound = 1,
    
    /// <summary>
    /// Validation error - maps to 400 Bad Request
    /// </summary>
    Validation = 2,
    
    /// <summary>
    /// Unauthorized access - maps to 401 Unauthorized
    /// </summary>
    Unauthorized = 3,
    
    /// <summary>
    /// Forbidden access - maps to 403 Forbidden
    /// </summary>
    Forbidden = 4,
    
    /// <summary>
    /// Conflict (e.g., duplicate resource) - maps to 409 Conflict
    /// </summary>
    Conflict = 5
}
