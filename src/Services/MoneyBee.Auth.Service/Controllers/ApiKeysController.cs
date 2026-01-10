using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Auth.Service.Data;
using MoneyBee.Auth.Service.DTOs;
using MoneyBee.Auth.Service.Entities;
using MoneyBee.Auth.Service.Helpers;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Controllers;

[ApiController]
[Route("api/auth/keys")]
public class ApiKeysController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        AuthDbContext context,
        ILogger<ApiKeysController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new API Key
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateApiKeyResponse>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResponse<CreateApiKeyResponse>.ErrorResponse("Name is required"));
        }

        var apiKey = ApiKeyHelper.GenerateApiKey();
        var keyHash = ApiKeyHelper.HashApiKey(apiKey);

        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            KeyHash = keyHash,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresInDays.HasValue 
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value) 
                : null
        };

        _context.ApiKeys.Add(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key created: {KeyId} - {KeyName}", entity.Id, entity.Name);

        var response = new CreateApiKeyResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            ApiKey = apiKey, // Only shown once!
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt
        };

        return CreatedAtAction(
            nameof(GetApiKey),
            new { id = entity.Id },
            ApiResponse<CreateApiKeyResponse>.SuccessResponse(response, "API Key created successfully"));
    }

    /// <summary>
    /// Get all API Keys (without actual keys)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ApiKeyDto>>), 200)]
    public async Task<IActionResult> GetAllApiKeys()
    {
        var keys = await _context.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        var dtos = keys.Select(k => new ApiKeyDto
        {
            Id = k.Id,
            Name = k.Name,
            Description = k.Description,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{k.KeyHash.Substring(0, Math.Min(28, k.KeyHash.Length))}")
        }).ToList();

        return Ok(ApiResponse<List<ApiKeyDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// Get specific API Key by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetApiKey(Guid id)
    {
        var key = await _context.ApiKeys.FindAsync(id);

        if (key == null)
        {
            return NotFound(ApiResponse<ApiKeyDto>.ErrorResponse("API Key not found"));
        }

        var dto = new ApiKeyDto
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{key.KeyHash.Substring(0, Math.Min(28, key.KeyHash.Length))}")
        };

        return Ok(ApiResponse<ApiKeyDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Update API Key details
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] UpdateApiKeyRequest request)
    {
        var key = await _context.ApiKeys.FindAsync(id);

        if (key == null)
        {
            return NotFound(ApiResponse<ApiKeyDto>.ErrorResponse("API Key not found"));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            key.Name = request.Name;

        if (request.Description != null)
            key.Description = request.Description;

        if (request.IsActive.HasValue)
            key.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        _logger.LogInformation("API Key updated: {KeyId}", id);

        var dto = new ApiKeyDto
        {
            Id = key.Id,
            Name = key.Name,
            Description = key.Description,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            MaskedKey = ApiKeyHelper.MaskApiKey($"mb_{key.KeyHash.Substring(0, Math.Min(28, key.KeyHash.Length))}")
        };

        return Ok(ApiResponse<ApiKeyDto>.SuccessResponse(dto, "API Key updated successfully"));
    }

    /// <summary>
    /// Delete API Key
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteApiKey(Guid id)
    {
        var key = await _context.ApiKeys.FindAsync(id);

        if (key == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("API Key not found"));
        }

        _context.ApiKeys.Remove(key);
        await _context.SaveChangesAsync();

        _logger.LogWarning("API Key deleted: {KeyId} - {KeyName}", id, key.Name);

        return NoContent();
    }

    /// <summary>
    /// Validate an API Key
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> ValidateApiKey([FromBody] string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || !ApiKeyHelper.IsValidApiKeyFormat(apiKey))
        {
            return Ok(ApiResponse<bool>.SuccessResponse(false, "Invalid API Key format"));
        }

        var keyHash = ApiKeyHelper.HashApiKey(apiKey);
        var exists = await _context.ApiKeys
            .AnyAsync(k => k.KeyHash == keyHash && k.IsActive && 
                          (!k.ExpiresAt.HasValue || k.ExpiresAt.Value > DateTime.UtcNow));

        return Ok(ApiResponse<bool>.SuccessResponse(exists, exists ? "Valid API Key" : "Invalid API Key"));
    }
}
