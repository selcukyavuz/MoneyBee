using Microsoft.EntityFrameworkCore;
using MoneyBee.Auth.Service.Infrastructure.Data;
using MoneyBee.Auth.Service.Domain.ApiKeys;

namespace MoneyBee.Auth.Service.Infrastructure.ApiKeys;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly AuthDbContext _context;

    public ApiKeyRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id)
    {
        return await _context.ApiKeys.FindAsync(id);
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
    {
        return await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash);
    }

    public async Task<IEnumerable<ApiKey>> GetAllAsync()
    {
        return await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
    }

    public async Task<ApiKey> CreateAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var apiKey = await GetByIdAsync(id);
        if (apiKey is null)
        {
            return false;
        }

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.ApiKeys.AnyAsync(k => k.Id == id);
    }
}
