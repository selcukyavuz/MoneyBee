using Microsoft.AspNetCore.Mvc;
using MoneyBee.Auth.Service.Application.DTOs;
using MoneyBee.Auth.Service.Application.Interfaces;
using MoneyBee.Common.Models;

namespace MoneyBee.Auth.Service.Controllers;

[ApiController]
[Route("api/auth/keys")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(
        IApiKeyService apiKeyService,
        ILogger<ApiKeysController> logger)
    {
        _apiKeyService = apiKeyService;
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
        var response = await _apiKeyService.CreateApiKeyAsync(request);

        return CreatedAtAction(
            nameof(GetApiKey),
            new { id = response.Id },
            ApiResponse<CreateApiKeyResponse>.SuccessResponse(response, "API Key created successfully"));
    }

    /// <summary>
    /// Get all API Keys (without actual keys)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApiKeyDto>>), 200)]
    public async Task<IActionResult> GetAllApiKeys()
    {
        var keys = await _apiKeyService.GetAllApiKeysAsync();
        return Ok(ApiResponse<IEnumerable<ApiKeyDto>>.SuccessResponse(keys));
    }

    /// <summary>
    /// Get specific API Key by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetApiKey(Guid id)
    {
        var dto = await _apiKeyService.GetApiKeyByIdAsync(id);

        if (dto == null)
        {
            return NotFound(ApiResponse<ApiKeyDto>.ErrorResponse("API Key not found"));
        }

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
        var dto = await _apiKeyService.UpdateApiKeyAsync(id, request);

        if (dto == null)
        {
            return NotFound(ApiResponse<ApiKeyDto>.ErrorResponse("API Key not found"));
        }

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
        var deleted = await _apiKeyService.DeleteApiKeyAsync(id);

        if (!deleted)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("API Key not found"));
        }

        return NoContent();
    }

    /// <summary>
    /// Validate an API Key
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> ValidateApiKey([FromBody] string apiKey)
    {
        var isValid = await _apiKeyService.ValidateApiKeyAsync(apiKey);
        return Ok(ApiResponse<bool>.SuccessResponse(isValid, isValid ? "Valid API Key" : "Invalid API Key"));
    }
}
