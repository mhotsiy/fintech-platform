using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Models.Responses;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Enums;
using FintechPlatform.Domain.Events;
using FintechPlatform.Domain.Repositories;

namespace FintechPlatform.Api.Services;

public class WithdrawalService : IWithdrawalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WithdrawalService> _logger;

    public WithdrawalService(IUnitOfWork unitOfWork, ILogger<WithdrawalService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WithdrawalResponse> CreateWithdrawalAsync(Guid merchantId, CreateWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating withdrawal for merchant {MerchantId}, amount {Amount} {Currency}", 
            merchantId, request.AmountInMinorUnits, request.Currency);

        // Verify merchant exists
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(merchantId, cancellationToken);
        if (merchant == null)
        {
            throw new InvalidOperationException($"Merchant {merchantId} not found");
        }

        // Start transaction for balance reservation
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get balance with lock (for update)
            var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyForUpdateAsync(
                merchantId, request.Currency.ToUpperInvariant(), cancellationToken);

            if (balance == null)
            {
                throw new InvalidOperationException($"No {request.Currency} balance found for merchant");
            }

            // Check sufficient available balance
            if (balance.AvailableBalanceInMinorUnits < request.AmountInMinorUnits)
            {
                throw new InvalidOperationException(
                    $"Insufficient balance. Available: {balance.AvailableBalanceInMinorUnits}, Requested: {request.AmountInMinorUnits}");
            }

            // Create withdrawal
            var withdrawal = new Withdrawal(
                merchantId,
                request.AmountInMinorUnits,
                request.Currency,
                request.BankAccountNumber,
                request.BankRoutingNumber);

            // Reserve balance (move from available to pending)
            balance.DeductFromAvailableBalance(request.AmountInMinorUnits);
            balance.AddToPendingBalance(request.AmountInMinorUnits);

            // Persist changes
            await _unitOfWork.Withdrawals.AddAsync(withdrawal, cancellationToken);
            
            // Create ledger entry
            var ledgerEntry = new LedgerEntry(
                merchantId,
                LedgerEntryType.WithdrawalRequested,
                request.AmountInMinorUnits,
                request.Currency,
                balance.AvailableBalanceInMinorUnits,
                withdrawal.Id);

            await _unitOfWork.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);

            // Publish event
            var withdrawalRequestedEvent = new WithdrawalRequestedEvent(
                withdrawal.Id,
                merchantId,
                request.AmountInMinorUnits,
                request.Currency,
                request.BankAccountNumber,
                request.BankRoutingNumber);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish to Kafka after commit (this should be injected IEventPublisher like PaymentService does)
            // For now, skipping Kafka publish - will be handled by Withdrawal Worker

            _logger.LogInformation("Withdrawal {WithdrawalId} created successfully for merchant {MerchantId}", 
                withdrawal.Id, merchantId);

            return ToResponse(withdrawal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create withdrawal for merchant {MerchantId}", merchantId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WithdrawalResponse?> GetWithdrawalByIdAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        var withdrawal = await _unitOfWork.Withdrawals.GetByIdAsync(withdrawalId, cancellationToken);
        
        if (withdrawal == null || withdrawal.MerchantId != merchantId)
        {
            return null;
        }

        return ToResponse(withdrawal);
    }

    public async Task<IEnumerable<WithdrawalResponse>> GetMerchantWithdrawalsAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        // Verify merchant exists
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(merchantId, cancellationToken);
        if (merchant == null)
        {
            throw new InvalidOperationException($"Merchant {merchantId} not found");
        }

        var withdrawals = await _unitOfWork.Withdrawals.GetByMerchantIdAsync(merchantId, cancellationToken);
        return withdrawals.Select(ToResponse);
    }

    public async Task<WithdrawalResponse> ProcessWithdrawalAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing withdrawal {WithdrawalId} for merchant {MerchantId}", withdrawalId, merchantId);

        var withdrawal = await _unitOfWork.Withdrawals.GetByIdAsync(withdrawalId, cancellationToken);
        
        if (withdrawal == null || withdrawal.MerchantId != merchantId)
        {
            throw new InvalidOperationException($"Withdrawal {withdrawalId} not found for merchant {merchantId}");
        }

        // In real implementation, this would integrate with banking API
        // For now, simulate external transaction ID
        var externalTxId = $"EXT-{Guid.NewGuid().ToString().Substring(0, 8)}";
        
        withdrawal.MarkAsProcessing(externalTxId);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Withdrawal {WithdrawalId} marked as processing with external ID {ExternalTxId}", 
            withdrawalId, externalTxId);

        return ToResponse(withdrawal);
    }

    public async Task<WithdrawalResponse> CancelWithdrawalAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling withdrawal {WithdrawalId} for merchant {MerchantId}", withdrawalId, merchantId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var withdrawal = await _unitOfWork.Withdrawals.GetByIdAsync(withdrawalId, cancellationToken);
            
            if (withdrawal == null || withdrawal.MerchantId != merchantId)
            {
                throw new InvalidOperationException($"Withdrawal {withdrawalId} not found for merchant {merchantId}");
            }

            // Cancel withdrawal
            withdrawal.Cancel();

            // Get balance with lock
            var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyForUpdateAsync(
                merchantId, withdrawal.Currency, cancellationToken);

            if (balance == null)
            {
                throw new InvalidOperationException($"Balance not found for merchant {merchantId} currency {withdrawal.Currency}");
            }

            // Release reserved balance (move from pending back to available)
            balance.DeductFromPendingBalance(withdrawal.AmountInMinorUnits);
            balance.AddToAvailableBalance(withdrawal.AmountInMinorUnits);

            // Create ledger entry
            var ledgerEntry = new LedgerEntry(
                merchantId,
                LedgerEntryType.WithdrawalCancelled,
                withdrawal.AmountInMinorUnits,
                withdrawal.Currency,
                balance.AvailableBalanceInMinorUnits,
                withdrawal.Id);

            await _unitOfWork.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);

            // Publish event
            var withdrawalCancelledEvent = new WithdrawalCancelledEvent
            {
                EventId = Guid.NewGuid(),
                WithdrawalId = withdrawal.Id,
                MerchantId = merchantId,
                AmountInMinorUnits = withdrawal.AmountInMinorUnits,
                Currency = withdrawal.Currency,
                OccurredAt = DateTime.UtcNow
            };

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish to Kafka after commit (this should be injected IEventPublisher)
            // For now, skipping Kafka publish

            _logger.LogInformation("Withdrawal {WithdrawalId} cancelled successfully", withdrawalId);

            return ToResponse(withdrawal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel withdrawal {WithdrawalId}", withdrawalId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static WithdrawalResponse ToResponse(Withdrawal withdrawal)
    {
        return new WithdrawalResponse
        {
            Id = withdrawal.Id,
            MerchantId = withdrawal.MerchantId,
            AmountInMinorUnits = withdrawal.AmountInMinorUnits,
            Currency = withdrawal.Currency,
            Status = withdrawal.Status.ToString(),
            BankAccountNumber = withdrawal.BankAccountNumber,
            BankRoutingNumber = withdrawal.BankRoutingNumber,
            ExternalTransactionId = withdrawal.ExternalTransactionId,
            FailureReason = withdrawal.FailureReason,
            CreatedAt = withdrawal.CreatedAt,
            ProcessedAt = withdrawal.ProcessedAt,
            CompletedAt = withdrawal.CompletedAt
        };
    }
}
