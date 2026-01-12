using System.ComponentModel.DataAnnotations;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Domain.Entities;

public class Customer
{
    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; private set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; private set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string NationalId { get; private set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; private set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; private set; }

    [Required]
    public CustomerType CustomerType { get; private set; }

    [Required]
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    public bool KycVerified { get; internal set; } = false;

    public string? TaxNumber { get; private set; }

    public string? Address { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; internal set; }

    public string? Email { get; private set; }

    // For EF Core
    private Customer() { }

    public static Customer Create(
        string firstName,
        string lastName,
        string nationalId,
        string phoneNumber,
        DateTime dateOfBirth,
        CustomerType customerType,
        string? taxNumber,
        string? address,
        string? email)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            NationalId = nationalId,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth,
            CustomerType = customerType,
            TaxNumber = taxNumber,
            Address = address,
            Email = email,
            Status = CustomerStatus.Active,
            KycVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        return customer;
    }

    public void UpdateStatus(CustomerStatus newStatus)
    {
        if (Status == newStatus)
            return;

        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateInformation(
        string firstName,
        string lastName,
        string phoneNumber,
        string? address,
        string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Address = address;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public void VerifyKyc()
    {
        KycVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkForDeletion()
    {
        // Domain event removed - publish integration event directly in service layer
    }

    public bool IsAdult()
    {
        var age = DateTime.Today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > DateTime.Today.AddYears(-age))
            age--;
        return age >= 18;
    }

    public bool CanPerformTransactions()
    {
        return Status == CustomerStatus.Active && IsAdult();
    }
}
