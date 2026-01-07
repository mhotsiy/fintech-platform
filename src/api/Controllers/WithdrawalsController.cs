using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Models.Responses;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/merchants/{merchantId:guid}/withdrawals")]
public class WithdrawalsController : ControllerBase
{
    private readonly IWithdrawalService _withdrawalService;
    private readonly ILogger<WithdrawalsController> _logger;

    public WithdrawalsController(IWithdrawalService withdrawalService, ILogger<WithdrawalsController> logger)
    {
        _withdrawalService = withdrawalService ?? throw new ArgumentNullException(nameof(withdrawalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<WithdrawalResponse>> CreateWithdrawal(
        Guid merchantId, 
        [FromBody] CreateWithdrawalRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating withdrawal for merchant {MerchantId}, amount {Amount} {Currency}", 
                merchantId, request.AmountInMinorUnits, request.Currency);
            
            var withdrawal = await _withdrawalService.CreateWithdrawalAsync(merchantId, request, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetWithdrawal), 
                new { merchantId, withdrawalId = withdrawal.Id }, 
                withdrawal);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid withdrawal creation request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Withdrawal creation failed");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WithdrawalResponse>>> GetWithdrawals(
        Guid merchantId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var withdrawals = await _withdrawalService.GetMerchantWithdrawalsAsync(merchantId, cancellationToken);
            return Ok(withdrawals);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Merchant {MerchantId} not found", merchantId);
            return NotFound(new { Error = ex.Message });
        }
    }

    [HttpGet("{withdrawalId:guid}")]
    public async Task<ActionResult<WithdrawalResponse>> GetWithdrawal(
        Guid merchantId, 
        Guid withdrawalId, 
        CancellationToken cancellationToken)
    {
        var withdrawal = await _withdrawalService.GetWithdrawalByIdAsync(merchantId, withdrawalId, cancellationToken);
        
        if (withdrawal == null)
        {
            return NotFound(new { Error = $"Withdrawal {withdrawalId} not found" });
        }

        return Ok(withdrawal);
    }

    [HttpPost("{withdrawalId:guid}/process")]
    public async Task<ActionResult<WithdrawalResponse>> ProcessWithdrawal(
        Guid merchantId, 
        Guid withdrawalId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var withdrawal = await _withdrawalService.ProcessWithdrawalAsync(merchantId, withdrawalId, cancellationToken);
            return Ok(withdrawal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot process withdrawal {WithdrawalId}", withdrawalId);
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("{withdrawalId:guid}/cancel")]
    public async Task<ActionResult<WithdrawalResponse>> CancelWithdrawal(
        Guid merchantId, 
        Guid withdrawalId, 
        CancellationToken cancellationToken)
    {
        try
        {
            var withdrawal = await _withdrawalService.CancelWithdrawalAsync(merchantId, withdrawalId, cancellationToken);
            return Ok(withdrawal);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel withdrawal {WithdrawalId}", withdrawalId);
            return BadRequest(new { Error = ex.Message });
        }
    }
}
