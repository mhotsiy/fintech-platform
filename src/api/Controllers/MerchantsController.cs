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

    [HttpGet("{merchantId:guid}/balances")]
    public async Task<ActionResult<IEnumerable<Models.Responses.BalanceResponse>>> GetMerchantBalances(Guid merchantId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting balances for merchant {MerchantId}", merchantId);
            var balances = await _merchantService.GetMerchantBalancesAsync(merchantId, cancellationToken);
            return Ok(balances);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Merchant {MerchantId} not found", merchantId);
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("{merchantId:guid}/balances/{currency}")]
    public async Task<ActionResult<Models.Responses.BalanceResponse>> GetMerchantBalance(Guid merchantId, string currency, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting {Currency} balance for merchant {MerchantId}", currency, merchantId);
            var balance = await _merchantService.GetMerchantBalanceAsync(merchantId, currency, cancellationToken);
            
            if (balance == null)
            {
                return NotFound(new { Error = $"No {currency} balance found for merchant" });
            }
            
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Merchant {MerchantId} not found", merchantId);
            return NotFound(new { Error = ex.Message });
        }
    }
}
