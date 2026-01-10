using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MoneyBee.Auth.Service.Data;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=auth_db;Username=moneybee;Password=moneybee123");

        return new AuthDbContext(optionsBuilder.Options);
    }
}
