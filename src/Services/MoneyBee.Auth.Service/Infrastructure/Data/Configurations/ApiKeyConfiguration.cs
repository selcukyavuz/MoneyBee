using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoneyBee.Auth.Service.Domain.ApiKeys;

namespace MoneyBee.Auth.Service.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ApiKey entity
/// </summary>
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("name");

        builder.Property(x => x.KeyHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("key_hash");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasColumnName("is_active");

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasColumnName("created_at");

        builder.Property(x => x.ExpiresAt)
            .IsRequired(false)
            .HasColumnName("expires_at");

        builder.Property(x => x.LastUsedAt)
            .IsRequired(false)
            .HasColumnName("last_used_at");

        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasMaxLength(500)
            .HasColumnName("description");

        // Indexes for performance
        builder.HasIndex(x => x.KeyHash)
            .IsUnique()
            .HasDatabaseName("IX_api_keys_key_hash");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_api_keys_is_active");
    }
}
