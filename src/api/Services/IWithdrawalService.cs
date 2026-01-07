using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Models.Responses;

namespace FintechPlatform.Api.Services;

public interface IWithdrawalService
{
    Task<WithdrawalResponse> CreateWithdrawalAsync(Guid merchantId, CreateWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<WithdrawalResponse?> GetWithdrawalByIdAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WithdrawalResponse>> GetMerchantWithdrawalsAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<WithdrawalResponse> ProcessWithdrawalAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default);
    Task<WithdrawalResponse> CancelWithdrawalAsync(Guid merchantId, Guid withdrawalId, CancellationToken cancellationToken = default);
}
