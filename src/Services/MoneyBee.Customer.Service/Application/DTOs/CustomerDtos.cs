using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Application.DTOs;

public class CreateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public CustomerType CustomerType { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}

public class UpdateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
}

public class UpdateCustomerStatusRequest
{
    public CustomerStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string NationalId { get; set; } = string.Empty;
    public string MaskedNationalId => MaskNationalId(NationalId);
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public int Age => CalculateAge(DateOfBirth);
    public CustomerType CustomerType { get; set; }
    public CustomerStatus Status { get; set; }
    public bool KycVerified { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

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

public class CustomerVerificationRequest
{
    public string NationalId { get; set; } = string.Empty;
}

public class CustomerVerificationResponse
{
    public bool Exists { get; set; }
    public Guid? CustomerId { get; set; }
    public CustomerStatus? Status { get; set; }
    public bool? KycVerified { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
}
