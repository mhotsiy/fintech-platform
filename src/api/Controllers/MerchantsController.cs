using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MerchantsController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly ILogger<MerchantsController> _logger;

    public MerchantsController(IMerchantService merchantService, ILogger<MerchantsController> logger)
    {
        _merchantService = merchantService ?? throw new ArgumentNullException(nameof(merchantService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new merchant account
    /// </summary>
    /// <param name="request">Merchant creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created merchant with initial zero balance</returns>
    /// <response code="201">Merchant created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Merchant with this email already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Get merchant details by ID
    /// </summary>
    /// <param name="id">The unique identifier of the merchant</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The merchant details including current balance</returns>
    /// <response code="200">Merchant found</response>
    /// <response code="404">Merchant not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MerchantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
