using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.DDD;
using MoneyBee.Common.Persistence;
using MoneyBee.Transfer.Service.Domain.Entities;
using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Infrastructure.Data;

public class TransferDbContext : DbContext
{
    public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Entities.Transfer> Transfers { get; set; }
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

        modelBuilder.Entity<Domain.Entities.Transfer>(entity =>
        {
            entity.ToTable("transfers");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.SenderId)
                .HasColumnName("sender_id")
                .IsRequired();
            
            entity.Property(e => e.ReceiverId)
                .HasColumnName("receiver_id")
                .IsRequired();
            
            entity.Property(e => e.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasConversion<int>()
                .IsRequired();
            
            entity.Property(e => e.AmountInTRY)
                .HasColumnName("amount_in_try")
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.ExchangeRate)
                .HasColumnName("exchange_rate")
                .HasPrecision(18, 6);
            
            entity.Property(e => e.TransactionFee)
                .HasColumnName("transaction_fee")
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.TransactionCode)
                .HasColumnName("transaction_code")
                .IsRequired()
                .HasMaxLength(8);
            
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();
            
            entity.Property(e => e.RiskLevel)
                .HasColumnName("risk_level")
                .HasConversion<int?>();
            
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(100);
            
            // Optimistic concurrency control
            entity.Property(e => e.RowVersion)
                .HasColumnName("row_version")
                .IsRowVersion()
                .IsConcurrencyToken();
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_at");
            
            entity.Property(e => e.CancelledAt)
                .HasColumnName("cancelled_at");
            
            entity.Property(e => e.CancellationReason)
                .HasColumnName("cancellation_reason")
                .HasMaxLength(500);
            
            entity.Property(e => e.ApprovalRequiredUntil)
                .HasColumnName("approval_required_until");
            
            entity.Property(e => e.SenderNationalId)
                .HasColumnName("sender_national_id")
                .HasMaxLength(11);
            
            entity.Property(e => e.ReceiverNationalId)
                .HasColumnName("receiver_national_id")
                .HasMaxLength(11);

            // Indexes
            entity.HasIndex(e => e.TransactionCode)
                .IsUnique();
            
            entity.HasIndex(e => e.IdempotencyKey)
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
            
            entity.HasIndex(e => e.SenderId);
            
            entity.HasIndex(e => e.ReceiverId);
            
            entity.HasIndex(e => e.Status);
            
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
