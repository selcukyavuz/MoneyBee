using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Persistence;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;
using MoneyBee.Common.Enums;

namespace MoneyBee.Customer.Service.Infrastructure.Data;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore base classes that are not entities
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Ignore<AggregateRoot>();

        // Configure OutboxMessage
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(200)
                .IsRequired();
            
            entity.Property(e => e.EventData)
                .HasColumnName("event_data")
                .IsRequired();
            
            entity.Property(e => e.OccurredOn)
                .HasColumnName("occurred_on");
            
            entity.Property(e => e.Published)
                .HasColumnName("published")
                .HasDefaultValue(false);
            
            entity.Property(e => e.PublishedAt)
                .HasColumnName("published_at");
            
            entity.Property(e => e.ProcessAttempts)
                .HasColumnName("process_attempts")
                .HasDefaultValue(0);
            
            entity.Property(e => e.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(2000);
            
            entity.Property(e => e.LastAttemptAt)
                .HasColumnName("last_attempt_at");
            
            // Indexes for efficient querying
            entity.HasIndex(e => new { e.Published, e.OccurredOn })
                .HasDatabaseName("ix_outbox_messages_published_occurred");
        });

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
