using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoneyBee.Transfer.Service.Infrastructure.Data;

public class TransferDbContextFactory : IDesignTimeDbContextFactory<TransferDbContext>
{
    public TransferDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TransferDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5434;Database=transfer_db;Username=moneybee;Password=moneybee123");

        return new TransferDbContext(optionsBuilder.Options);
    }
}
