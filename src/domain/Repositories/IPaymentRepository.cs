using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public class PaymentFilter
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public long? MinAmount { get; set; }
    public long? MaxAmount { get; set; }
    public List<PaymentStatus>? Statuses { get; set; }
    public string? Search { get; set; }
}

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByMerchantIdAndStatusAsync(Guid merchantId, PaymentStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByMerchantIdWithFilterAsync(Guid merchantId, PaymentFilter filter, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}
