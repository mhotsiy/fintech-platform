using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace FintechPlatform.Workers.Services;

/// <summary>
/// Service to handle payment workflow operations for workers
/// </summary>
public class PaymentWorkflowService
{
    private readonly ILogger<PaymentWorkflowService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentWorkflowService(
        ILogger<PaymentWorkflowService> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> GetMerchantCompletedPaymentCountAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdAsync(merchantId, cancellationToken);
        return payments.Count(p => p.Status == Domain.Entities.PaymentStatus.Completed);
    }

    public async Task CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
        {
            throw new InvalidOperationException($"Payment {paymentId} not found");
        }

        if (payment.Status != Domain.Entities.PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Payment {paymentId} cannot be completed. Current status: {payment.Status}");
        }

        // Update balance
        var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyAsync(
            payment.MerchantId,
            payment.Currency,
            cancellationToken);

        if (balance == null)
        {
            throw new InvalidOperationException($"Balance not found for merchant {payment.MerchantId}");
        }

        balance.AddToAvailableBalance(payment.AmountInMinorUnits);
        await _unitOfWork.Balances.UpdateAsync(balance, cancellationToken);

        // Create ledger entry
        var ledgerEntry = new Domain.Entities.LedgerEntry(
            payment.MerchantId,
            Domain.Entities.LedgerEntryType.PaymentReceived,
            payment.AmountInMinorUnits,
            payment.Currency,
            balance.AvailableBalanceInMinorUnits,
            payment.Id,
            null,
            $"Payment completed: {payment.Description}",
            null);

        await _unitOfWork.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);

        // Update payment status
        payment.Complete();
        await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);

        // Commit transaction
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment {PaymentId} completed successfully by worker", paymentId);
    }
}
