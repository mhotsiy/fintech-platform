using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MerchantsController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly ILogger<MerchantsController> _logger;

    public MerchantsController(IMerchantService merchantService, ILogger<MerchantsController> logger)
    {
        _merchantService = merchantService ?? throw new ArgumentNullException(nameof(merchantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<MerchantDto>> CreateMerchant([FromBody] CreateMerchantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating merchant with email {Email}", request.Email);
            var merchant = await _merchantService.CreateMerchantAsync(request.Name, request.Email, cancellationToken);
            return CreatedAtAction(nameof(GetMerchant), new { id = merchant.Id }, merchant);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid merchant creation request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Merchant creation failed");
            return Conflict(new { Error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MerchantDto>> GetMerchant(Guid id, CancellationToken cancellationToken)
    {
        var merchant = await _merchantService.GetMerchantByIdAsync(id, cancellationToken);
        
        if (merchant == null)
        {
            return NotFound();
        }

        return Ok(merchant);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MerchantDto>>> GetActiveMerchants(CancellationToken cancellationToken)
    {
        var merchants = await _merchantService.GetActiveMerchantsAsync(cancellationToken);
        return Ok(merchants);
    }
}
