using Microsoft.EntityFrameworkCore;
using MoneyBee.Auth.Service.Domain.Entities;

namespace MoneyBee.Auth.Service.Infrastructure.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.KeyHash)
                .HasColumnName("key_hash")
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");
            
            entity.Property(e => e.LastUsedAt)
                .HasColumnName("last_used_at");
            
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.HasIndex(e => e.KeyHash)
                .IsUnique();
        });
    }
}
