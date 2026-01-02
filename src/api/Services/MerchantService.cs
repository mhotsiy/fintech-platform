using FintechPlatform.Api.Mapping;
using FintechPlatform.Api.Models.Dtos;
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
        
        // Create default USD balance for the merchant
        var usdBalance = new Balance(merchant.Id, "USD");
        await _unitOfWork.Balances.AddAsync(usdBalance, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created merchant {MerchantId} with email {Email} and USD balance", merchant.Id, email);

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
}
