using System.ComponentModel.DataAnnotations;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Domain.Customers;

/// <summary>
/// Represents a customer entity with KYC verification and business rules
/// </summary>
public class Customer
{
    /// <summary>
    /// Gets the unique identifier of the customer
    /// </summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the first name of the customer
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the last name of the customer
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the national ID number (Turkish TC Kimlik No)
    /// </summary>
    [Required]
    [MaxLength(11)]
    public string NationalId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the phone number with country code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the date of birth for age verification
    /// </summary>
    [Required]
    public DateTime DateOfBirth { get; private set; }

    /// <summary>
    /// Gets the customer type (Individual or Corporate)
    /// </summary>
    [Required]
    public CustomerType CustomerType { get; private set; }

    /// <summary>
    /// Gets the current status of the customer
    /// </summary>
    [Required]
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    /// <summary>
    /// Gets whether the customer has completed KYC verification
    /// </summary>
    public bool KycVerified { get; internal set; } = false;

    /// <summary>
    /// Gets the tax number for corporate customers
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// Gets the address of the customer
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Gets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; internal set; }

    /// <summary>
    /// Gets the email address of the customer
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
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
