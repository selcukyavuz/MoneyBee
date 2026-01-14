using Microsoft.EntityFrameworkCore;
using MoneyBee.Transfer.Service.Domain.Transfers;
using MoneyBee.Transfer.Service.Infrastructure.Data.Configurations;

namespace MoneyBee.Transfer.Service.Infrastructure.Data;

public class TransferDbContext : DbContext
{
    public TransferDbContext(DbContextOptions<TransferDbContext> options) : base(options)
    {
    }

    public DbSet<Domain.Transfers.Transfer> Transfers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from separate configuration classes
        modelBuilder.ApplyConfiguration(new TransferConfiguration());
    }
}
