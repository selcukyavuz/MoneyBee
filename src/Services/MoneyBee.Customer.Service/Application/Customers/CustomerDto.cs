using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.Customers;

public record CustomerDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public CustomerType CustomerType { get; init; }
    public CustomerStatus Status { get; init; }
    public bool KycVerified { get; init; }
    public string? TaxNumber { get; init; }
    public string? Address { get; init; }
    public string? Email { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public string FullName => $"{FirstName} {LastName}";
    public string MaskedNationalId => MaskNationalId(NationalId);
    public int Age => CalculateAge(DateOfBirth);

    private static string MaskNationalId(string nationalId)
    {
        if (string.IsNullOrEmpty(nationalId) || nationalId.Length < 11)
            return "***********";
        
        return $"{nationalId.Substring(0, 3)}****{nationalId.Substring(9)}";
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}
