using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MoneyBee.Transfer.Service.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Transfer entity
/// </summary>
public class TransferConfiguration : IEntityTypeConfiguration<Domain.Transfers.Transfer>
{
    public void Configure(EntityTypeBuilder<Domain.Transfers.Transfer> builder)
    {
        // Table
        builder.ToTable("transfers");

        // Primary Key
        builder.HasKey(e => e.Id);

        // Properties with column names (matching existing schema)
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.SenderId)
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(e => e.ReceiverId)
            .HasColumnName("receiver_id")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasColumnName("currency")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.AmountInTRY)
            .HasColumnName("amount_in_try")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.ExchangeRate)
            .HasColumnName("exchange_rate")
            .HasPrecision(18, 6);

        builder.Property(e => e.TransactionFee)
            .HasColumnName("transaction_fee")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.TransactionCode)
            .HasColumnName("transaction_code")
            .IsRequired()
            .HasMaxLength(8);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.RiskLevel)
            .HasColumnName("risk_level")
            .HasConversion<int?>();

        builder.Property(e => e.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        // Optimistic concurrency control
        builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(e => e.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(e => e.ApprovalRequiredUntil)
            .HasColumnName("approval_required_until");

        builder.Property(e => e.SenderNationalId)
            .HasColumnName("sender_national_id")
            .HasMaxLength(11);

        builder.Property(e => e.ReceiverNationalId)
            .HasColumnName("receiver_national_id")
            .HasMaxLength(11);

        // Indexes
        builder.HasIndex(e => e.TransactionCode)
            .IsUnique();

        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(e => e.SenderId);

        builder.HasIndex(e => e.ReceiverId);

        builder.HasIndex(e => e.Status);

        builder.HasIndex(e => e.CreatedAt);
    }
}
