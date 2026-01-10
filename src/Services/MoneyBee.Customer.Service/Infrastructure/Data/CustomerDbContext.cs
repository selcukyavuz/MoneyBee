using Microsoft.EntityFrameworkCore;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("customers");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.FirstName)
                .HasColumnName("first_name")
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.LastName)
                .HasColumnName("last_name")
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.NationalId)
                .HasColumnName("national_id")
                .IsRequired()
                .HasMaxLength(11);
            
            entity.Property(e => e.PhoneNumber)
                .HasColumnName("phone_number")
                .IsRequired()
                .HasMaxLength(20);
            
            entity.Property(e => e.DateOfBirth)
                .HasColumnName("date_of_birth")
                .IsRequired();
            
            entity.Property(e => e.CustomerType)
                .HasColumnName("customer_type")
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(CustomerStatus.Active);
            
            entity.Property(e => e.KycVerified)
                .HasColumnName("kyc_verified")
                .HasDefaultValue(false);
            
            entity.Property(e => e.TaxNumber)
                .HasColumnName("tax_number")
                .HasMaxLength(20);
            
            entity.Property(e => e.Address)
                .HasColumnName("address")
                .HasMaxLength(500);
            
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(100);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            // Indexes
            entity.HasIndex(e => e.NationalId)
                .IsUnique();
            
            entity.HasIndex(e => e.PhoneNumber);
            
            entity.HasIndex(e => e.Status);
        });
    }
}
