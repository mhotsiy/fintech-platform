using FintechPlatform.Domain.Events;
using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data.Repositories;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace FintechPlatform.Workers.Services;

/// <summary>
/// Service to handle payment workflow operations for workers
/// </summary>
public class PaymentWorkflowService
{
    private readonly ILogger<PaymentWorkflowService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public PaymentWorkflowService(
        ILogger<PaymentWorkflowService> logger,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<int> GetMerchantCompletedPaymentCountAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdAsync(merchantId, cancellationToken);
        return payments.Count(p => p.Status == Domain.Entities.PaymentStatus.Completed);
    }

    public async Task CompletePaymentAsync(Guid paymentId, Domain.Entities.CompletionSource completionSource = Domain.Entities.CompletionSource.Manual, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
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

            // Update payment status
            payment.Complete(completionSource);

            // Update balance
            var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyAsync(
                payment.MerchantId,
                payment.Currency,
                cancellationToken);

            if (balance == null)
            {
                // Create balance if it doesn't exist
                balance = new Domain.Entities.Balance(payment.MerchantId, payment.Currency);
                await _unitOfWork.Balances.AddAsync(balance, cancellationToken);
                _logger.LogInformation("Created new balance for merchant {MerchantId} in {Currency}", payment.MerchantId, payment.Currency);
            }

            balance.AddToAvailableBalance(payment.AmountInMinorUnits);

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

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Payment {PaymentId} completed successfully by worker", paymentId);

            // Publish PaymentCompletedEvent after successful commit
            var paymentCompletedEvent = new PaymentCompletedEvent(
                payment.Id,
                payment.MerchantId,
                payment.AmountInMinorUnits,
                payment.Currency,
                balance.AvailableBalanceInMinorUnits,
                ledgerEntry.Id);

            await _eventPublisher.PublishAsync("payment-events", paymentCompletedEvent, cancellationToken);
            _logger.LogInformation("Published PaymentCompletedEvent for payment {PaymentId}", paymentId);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
