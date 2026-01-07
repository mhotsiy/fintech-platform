using FintechPlatform.Api.Mapping;
using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Events;
using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork, 
        IEventPublisher eventPublisher,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentDto> CreatePaymentAsync(Guid merchantId, long amountInMinorUnits, string currency, string? externalReference, string? description, CancellationToken cancellationToken = default)
    {
        var merchantExists = await _unitOfWork.Merchants.ExistsAsync(merchantId, cancellationToken);
        if (!merchantExists)
        {
            throw new InvalidOperationException($"Merchant {merchantId} does not exist");
        }

        var payment = new Payment(merchantId, amountInMinorUnits, currency, externalReference, description);
        
        await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created payment {PaymentId} for merchant {MerchantId}", payment.Id, merchantId);

        // Publish event after successful DB commit
        var paymentCreatedEvent = new PaymentCreatedEvent(
            payment.Id,
            payment.MerchantId,
            payment.AmountInMinorUnits,
            payment.Currency,
            payment.ExternalReference,
            payment.Description
        );

        await _eventPublisher.PublishAsync("payment-events", paymentCreatedEvent, cancellationToken);

        return payment.ToDto();
    }

    public async Task<List<PaymentDto>> CreateBulkPaymentsAsync(Guid merchantId, List<CreatePaymentRequest> payments, CancellationToken cancellationToken = default)
    {
        var merchantExists = await _unitOfWork.Merchants.ExistsAsync(merchantId, cancellationToken);
        if (!merchantExists)
        {
            throw new InvalidOperationException($"Merchant {merchantId} does not exist");
        }

        _logger.LogInformation("Creating {Count} bulk payments for merchant {MerchantId}", payments.Count, merchantId);

        var createdPayments = new List<PaymentDto>();
        var paymentEvents = new List<PaymentCreatedEvent>();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var paymentRequest in payments)
            {
                var payment = new Payment(
                    merchantId,
                    paymentRequest.AmountInMinorUnits,
                    paymentRequest.Currency,
                    paymentRequest.ExternalReference,
                    paymentRequest.Description);

                await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
                createdPayments.Add(payment.ToDto());

                // Prepare event
                paymentEvents.Add(new PaymentCreatedEvent(
                    payment.Id,
                    payment.MerchantId,
                    payment.AmountInMinorUnits,
                    payment.Currency,
                    payment.ExternalReference,
                    payment.Description));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish events after successful commit
            foreach (var paymentEvent in paymentEvents)
            {
                await _eventPublisher.PublishAsync("payment-events", paymentEvent, cancellationToken);
            }

            _logger.LogInformation("Successfully created {Count} bulk payments for merchant {MerchantId}", 
                createdPayments.Count, merchantId);

            return createdPayments;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(id, cancellationToken);
        return payment?.ToDto();
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdAsync(merchantId, cancellationToken);
        return payments.Select(p => p.ToDto());
    }

    public async Task<IEnumerable<PaymentDto>> GetPaymentsByMerchantIdWithFilterAsync(Guid merchantId, Domain.Repositories.PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdWithFilterAsync(merchantId, filter, cancellationToken);
        return payments.Select(p => p.ToDto());
    }

    public async Task<string> ExportPaymentsToCsvAsync(Guid merchantId, Domain.Repositories.PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdWithFilterAsync(merchantId, filter, cancellationToken);
        
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("ID,Merchant ID,Amount,Currency,Status,Description,External Reference,Created At,Completed At,Completed By,Refunded At,Refund Amount,Refund Reason");

        foreach (var payment in payments)
        {
            var amount = payment.AmountInMinorUnits / 100.0m;
            var refundAmount = payment.RefundedAmountInMinorUnits.HasValue 
                ? (payment.RefundedAmountInMinorUnits.Value / 100.0m).ToString("F2")
                : "";

            csv.AppendLine($"{payment.Id},{payment.MerchantId},{amount:F2},{payment.Currency},{payment.Status}," +
                          $"\"{payment.Description?.Replace("\"", "\"\"") ?? ""}\",\"{payment.ExternalReference ?? ""}\"," +
                          $"{payment.CreatedAt:yyyy-MM-dd HH:mm:ss},{payment.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                          $"{payment.CompletedBy?.ToString() ?? ""},{payment.RefundedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                          $"{refundAmount},\"{payment.RefundReason?.Replace("\"", "\"\"") ?? ""}\"");
        }

        return csv.ToString();
    }

    public async Task<PaymentDto> CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                throw new InvalidOperationException($"Payment {paymentId} not found");
            }

            payment.Complete();
            await _unitOfWork.Payments.UpdateAsync(payment, cancellationToken);

            var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyForUpdateAsync(payment.MerchantId, payment.Currency, cancellationToken);
            if (balance == null)
            {
                balance = new Balance(payment.MerchantId, payment.Currency);
                await _unitOfWork.Balances.AddAsync(balance, cancellationToken);
            }

            balance.AddToAvailableBalance(payment.AmountInMinorUnits);
            await _unitOfWork.Balances.UpdateAsync(balance, cancellationToken);

            var ledgerEntry = LedgerEntry.CreateForPayment(
                payment.MerchantId,
                payment.Id,
                payment.AmountInMinorUnits,
                payment.Currency,
                balance.AvailableBalanceInMinorUnits
            );

            await _unitOfWork.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Completed payment {PaymentId}, updated balance for merchant {MerchantId}", paymentId, payment.MerchantId);

            // Publish event after successful DB commit
            var paymentCompletedEvent = new PaymentCompletedEvent(
                payment.Id,
                payment.MerchantId,
                payment.AmountInMinorUnits,
                payment.Currency,
                balance.AvailableBalanceInMinorUnits,
                ledgerEntry.Id
            );

            await _eventPublisher.PublishAsync("payment-events", paymentCompletedEvent, cancellationToken);

            return payment.ToDto();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict while completing payment {PaymentId}", paymentId);
            throw new InvalidOperationException("Balance was modified by another transaction. Please retry.", ex);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PaymentDto> RefundPaymentAsync(Guid merchantId, Guid paymentId, long? refundAmountInMinorUnits = null, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refunding payment {PaymentId} for merchant {MerchantId}, amount: {Amount}, reason: {Reason}", 
            paymentId, merchantId, refundAmountInMinorUnits, reason);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId, cancellationToken);
            
            if (payment == null || payment.MerchantId != merchantId)
            {
                throw new InvalidOperationException($"Payment {paymentId} not found for merchant {merchantId}");
            }

            // Default to full refund if amount not specified
            var actualRefundAmount = refundAmountInMinorUnits ?? payment.AmountInMinorUnits;

            // Refund the payment
            payment.Refund(actualRefundAmount, reason);

            // Get balance with lock
            var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyForUpdateAsync(
                merchantId, payment.Currency, cancellationToken);

            if (balance == null)
            {
                throw new InvalidOperationException($"Balance not found for merchant {merchantId} currency {payment.Currency}");
            }

            // Deduct refund amount from available balance
            balance.DeductFromAvailableBalance(actualRefundAmount);

            // Create ledger entry
            var ledgerEntry = new LedgerEntry(
                merchantId,
                LedgerEntryType.PaymentRefunded,
                -actualRefundAmount, // Negative for debit
                payment.Currency,
                balance.AvailableBalanceInMinorUnits,
                payment.Id,
                null,
                $"Refund: {reason ?? "No reason provided"}",
                null);

            await _unitOfWork.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish event after successful commit
            var refundedEvent = new PaymentRefundedEvent(
                payment.Id,
                payment.MerchantId,
                actualRefundAmount,
                payment.Currency,
                balance.AvailableBalanceInMinorUnits,
                ledgerEntry.Id,
                reason);

            await _eventPublisher.PublishAsync("payment-events", refundedEvent, cancellationToken);

            _logger.LogInformation("Payment {PaymentId} refunded successfully, balance updated for merchant {MerchantId}", 
                paymentId, merchantId);

            return payment.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund payment {PaymentId}", paymentId);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
