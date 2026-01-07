using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;

namespace FintechPlatform.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(Guid merchantId, long amountInMinorUnits, string currency, string? externalReference, string? description, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetPaymentsByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetPaymentsByMerchantIdWithFilterAsync(Guid merchantId, Domain.Repositories.PaymentFilter filter, CancellationToken cancellationToken = default);
    Task<PaymentDto> CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<PaymentDto> RefundPaymentAsync(Guid merchantId, Guid paymentId, long? refundAmountInMinorUnits = null, string? reason = null, CancellationToken cancellationToken = default);
    Task<List<PaymentDto>> CreateBulkPaymentsAsync(Guid merchantId, List<CreatePaymentRequest> payments, CancellationToken cancellationToken = default);
    Task<string> ExportPaymentsToCsvAsync(Guid merchantId, Domain.Repositories.PaymentFilter filter, CancellationToken cancellationToken = default);
}

