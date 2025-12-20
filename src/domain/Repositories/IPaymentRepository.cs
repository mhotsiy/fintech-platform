using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByMerchantIdAndStatusAsync(Guid merchantId, PaymentStatus status, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}
