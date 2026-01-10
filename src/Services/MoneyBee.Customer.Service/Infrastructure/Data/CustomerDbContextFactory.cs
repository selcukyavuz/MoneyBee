using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoneyBee.Customer.Service.Infrastructure.Data;

public class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomerDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=customer_db;Username=moneybee;Password=moneybee123");

        return new CustomerDbContext(optionsBuilder.Options);
    }
}
