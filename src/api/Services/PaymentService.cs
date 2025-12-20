using FintechPlatform.Api.Mapping;
using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork unitOfWork, ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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

        return payment.ToDto();
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
}
