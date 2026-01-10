namespace MoneyBee.IntegrationTests.Shared;

// Shared DTOs for integration tests
public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status // String to match JSON serialization of Status enum
);

public record TransferDto(
    Guid Id,
    Guid SenderCustomerId,
    Guid ReceiverCustomerId,
    decimal Amount,
    string Currency,
    string Status,
    decimal? ConvertedAmount
);

public record ApiKeyResponse(string Key);
