using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Infrastructure.Data;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Customers.Customer;

namespace MoneyBee.Customer.Service.Infrastructure.Customers;

public class CustomerRepository : ICustomerRepository
{
    private readonly CustomerDbContext _context;

    public CustomerRepository(CustomerDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<CustomerEntity?> GetByNationalIdAsync(string nationalId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.NationalId == nationalId);
    }

    public async Task<IEnumerable<CustomerEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 50)
    {
        return await _context.Customers
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerEntity>> GetUnverifiedKycCustomersAsync(int hours = 24)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-hours);
        return await _context.Customers
            .Where(c => !c.KycVerified && c.CreatedAt > cutoffDate)
            .ToListAsync();
    }

    public async Task<CustomerEntity> CreateAsync(CustomerEntity customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<CustomerEntity> UpdateAsync(CustomerEntity customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await GetByIdAsync(id);
        if (customer is null)
        {
            return false;
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByNationalIdAsync(string nationalId)
    {
        return await _context.Customers.AnyAsync(c => c.NationalId == nationalId);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Customers.CountAsync();
    }
}
