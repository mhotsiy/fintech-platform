using FintechPlatform.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

/// <summary>
/// Admin endpoints for system health checks and diagnostics
/// In production, these would be protected with authentication/authorization
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IUnitOfWork unitOfWork, ILogger<AdminController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Verify that merchant balance matches ledger (data integrity check)
    /// </summary>
    /// <param name="merchantId">Merchant ID to verify</param>
    /// <param name="currency">Currency code (e.g., USD)</param>
    /// <returns>Verification result showing if balance matches ledger</returns>
    /// <response code="200">Verification completed</response>
    [HttpGet("verify-balance/{merchantId:guid}")]
    [ProducesResponseType(typeof(BalanceVerificationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BalanceVerificationResult>> VerifyBalance(
        Guid merchantId, 
        [FromQuery] string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        // Get balance from Balance table
        var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyAsync(
            merchantId, 
            currency, 
            cancellationToken);

        var balanceTableValue = balance?.AvailableBalanceInMinorUnits ?? 0;

        // Calculate balance from immutable ledger (source of truth)
        var ledgerBalance = await _unitOfWork.LedgerEntries.GetBalanceFromLedgerAsync(
            merchantId, 
            currency, 
            cancellationToken);

        var isValid = balanceTableValue == ledgerBalance;

        if (!isValid)
        {
            _logger.LogError(
                "[MISMATCH] Balance mismatch for merchant {MerchantId} in {Currency}: Balance table = {BalanceTable}, Ledger = {Ledger}",
                merchantId,
                currency,
                balanceTableValue,
                ledgerBalance);
        }
        else
        {
            _logger.LogInformation(
                "[VERIFIED] Balance verified for merchant {MerchantId} in {Currency}: {Balance}",
                merchantId,
                currency,
                balanceTableValue);
        }

        return Ok(new BalanceVerificationResult
        {
            MerchantId = merchantId,
            Currency = currency,
            BalanceTableValue = balanceTableValue,
            LedgerCalculatedValue = ledgerBalance,
            IsValid = isValid,
            DifferenceInMinorUnits = ledgerBalance - balanceTableValue,
            Message = isValid 
                ? "[OK] Balance matches ledger - data integrity confirmed" 
                : "[ERROR] Balance DOES NOT match ledger - data corruption detected!"
        });
    }

    /// <summary>
    /// Get merchant's complete transaction history from ledger
    /// Useful for debugging balance discrepancies
    /// </summary>
    /// <param name="merchantId">Merchant ID</param>
    /// <param name="limit">Max number of entries to return</param>
    /// <returns>Ledger entries showing complete audit trail</returns>
    [HttpGet("ledger-history/{merchantId:guid}")]
    [ProducesResponseType(typeof(LedgerHistoryResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<LedgerHistoryResult>> GetLedgerHistory(
        Guid merchantId,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var entries = await _unitOfWork.LedgerEntries.GetByMerchantIdAsync(
            merchantId, 
            limit, 
            0, 
            cancellationToken);

        var orderedEntries = entries
            .OrderBy(e => e.CreatedAt)
            .Select(e => new LedgerEntryDto
            {
                Id = e.Id,
                EntryType = e.EntryType.ToString(),
                AmountInMinorUnits = e.AmountInMinorUnits,
                AmountInMajorUnits = e.AmountInMinorUnits / 100.0m,
                Currency = e.Currency,
                BalanceAfterInMinorUnits = e.BalanceAfterInMinorUnits,
                BalanceAfterInMajorUnits = e.BalanceAfterInMinorUnits / 100.0m,
                RelatedPaymentId = e.RelatedPaymentId,
                RelatedWithdrawalId = e.RelatedWithdrawalId,
                Description = e.Description,
                CreatedAt = e.CreatedAt
            })
            .ToList();

        return Ok(new LedgerHistoryResult
        {
            MerchantId = merchantId,
            TotalEntries = orderedEntries.Count,
            Entries = orderedEntries
        });
    }
}

public class BalanceVerificationResult
{
    public required Guid MerchantId { get; init; }
    public required string Currency { get; init; }
    public required long BalanceTableValue { get; init; }
    public required long LedgerCalculatedValue { get; init; }
    public required bool IsValid { get; init; }
    public required long DifferenceInMinorUnits { get; init; }
    public required string Message { get; init; }
}

public class LedgerHistoryResult
{
    public required Guid MerchantId { get; init; }
    public required int TotalEntries { get; init; }
    public required List<LedgerEntryDto> Entries { get; init; }
}

public class LedgerEntryDto
{
    public required Guid Id { get; init; }
    public required string EntryType { get; init; }
    public required long AmountInMinorUnits { get; init; }
    public required decimal AmountInMajorUnits { get; init; }
    public required string Currency { get; init; }
    public required long BalanceAfterInMinorUnits { get; init; }
    public required decimal BalanceAfterInMajorUnits { get; init; }
    public Guid? RelatedPaymentId { get; init; }
    public Guid? RelatedWithdrawalId { get; init; }
    public string? Description { get; init; }
    public required DateTime CreatedAt { get; init; }
}
