using FintechPlatform.Api.Models.Dtos;

namespace FintechPlatform.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(Guid merchantId, long amountInMinorUnits, string currency, string? externalReference, string? description, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PaymentDto>> GetPaymentsByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<PaymentDto> CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
