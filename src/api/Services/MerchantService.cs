using FintechPlatform.Api.Mapping;
using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Responses;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Enums;
using FintechPlatform.Domain.Repositories;

namespace FintechPlatform.Api.Services;

public class MerchantService : IMerchantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MerchantService> _logger;

    public MerchantService(IUnitOfWork unitOfWork, ILogger<MerchantService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MerchantDto> CreateMerchantAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        var existingMerchant = await _unitOfWork.Merchants.GetByEmailAsync(email, cancellationToken);
        if (existingMerchant != null)
        {
            throw new InvalidOperationException($"Merchant with email {email} already exists");
        }

        var merchant = new Merchant(name, email);
        await _unitOfWork.Merchants.AddAsync(merchant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created merchant {MerchantId} with email {Email}", merchant.Id, email);

        return merchant.ToDto();
    }

    public async Task<MerchantDto?> GetMerchantByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(id, cancellationToken);
        return merchant?.ToDto();
    }

    public async Task<IEnumerable<MerchantDto>> GetActiveMerchantsAsync(CancellationToken cancellationToken = default)
    {
        var merchants = await _unitOfWork.Merchants.GetAllActiveAsync(cancellationToken);
        return merchants.Select(m => m.ToDto());
    }

    public async Task<IEnumerable<BalanceResponse>> GetMerchantBalancesAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        // Verify merchant exists
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(merchantId, cancellationToken);
        if (merchant == null)
        {
            throw new InvalidOperationException($"Merchant {merchantId} not found");
        }

        var balances = await _unitOfWork.Balances.GetByMerchantIdAsync(merchantId, cancellationToken);
        
        return balances.Select(b => new BalanceResponse
        {
            Id = b.Id,
            MerchantId = b.MerchantId,
            Currency = b.Currency,
            AvailableBalanceInMinorUnits = b.AvailableBalanceInMinorUnits,
            PendingBalanceInMinorUnits = b.PendingBalanceInMinorUnits,
            TotalBalanceInMinorUnits = b.AvailableBalanceInMinorUnits + b.PendingBalanceInMinorUnits,
            LastUpdated = b.UpdatedAt
        });
    }

    public async Task<BalanceResponse?> GetMerchantBalanceAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default)
    {
        // Verify merchant exists
        var merchant = await _unitOfWork.Merchants.GetByIdAsync(merchantId, cancellationToken);
        if (merchant == null)
        {
            throw new InvalidOperationException($"Merchant {merchantId} not found");
        }

        var balance = await _unitOfWork.Balances.GetByMerchantIdAndCurrencyAsync(merchantId, currency.ToUpperInvariant(), cancellationToken);
        
        if (balance == null)
        {
            return null;
        }

        return new BalanceResponse
        {
            Id = balance.Id,
            MerchantId = balance.MerchantId,
            Currency = balance.Currency,
            AvailableBalanceInMinorUnits = balance.AvailableBalanceInMinorUnits,
            PendingBalanceInMinorUnits = balance.PendingBalanceInMinorUnits,
            TotalBalanceInMinorUnits = balance.AvailableBalanceInMinorUnits + balance.PendingBalanceInMinorUnits,
            LastUpdated = balance.UpdatedAt
        };
    }
}
