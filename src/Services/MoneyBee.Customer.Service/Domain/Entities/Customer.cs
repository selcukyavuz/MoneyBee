using System.ComponentModel.DataAnnotations;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Domain.Entities;

public class Customer
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public CustomerType CustomerType { get; set; }

    [Required]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    public bool KycVerified { get; set; } = false;

    public string? TaxNumber { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string? Email { get; set; }
}
