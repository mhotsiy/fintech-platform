using FintechPlatform.Api.Controllers;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;

namespace FintechPlatform.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetMerchantAnalyticsAsync(Guid merchantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IUnitOfWork unitOfWork, ILogger<AnalyticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AnalyticsDto> GetMerchantAnalyticsAsync(
        Guid merchantId, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        var payments = await _unitOfWork.Payments.GetByMerchantIdWithFilterAsync(
            merchantId,
            new Domain.Repositories.PaymentFilter
            {
                DateFrom = fromDate,
                DateTo = toDate
            },
            cancellationToken);

        var paymentsList = payments.ToList();
        var totalPayments = paymentsList.Count;
        var completedPayments = paymentsList.Count(p => p.Status == PaymentStatus.Completed);
        var pendingPayments = paymentsList.Count(p => p.Status == PaymentStatus.Pending);
        var refundedPayments = paymentsList.Count(p => p.Status == PaymentStatus.Refunded);

        var totalRevenue = paymentsList
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.AmountInMinorUnits) / 100.0m;

        var averageAmount = completedPayments > 0
            ? totalRevenue / completedPayments
            : 0;

        var successRate = totalPayments > 0
            ? (completedPayments / (decimal)totalPayments) * 100
            : 0;

        // Daily revenue aggregation
        var dailyRevenue = paymentsList
            .Where(p => p.Status == PaymentStatus.Completed && p.CompletedAt.HasValue)
            .GroupBy(p => p.CompletedAt!.Value.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(p => p.AmountInMinorUnits) / 100.0m,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        // Status distribution
        var statusDistribution = paymentsList
            .GroupBy(p => p.Status)
            .Select(g => new PaymentStatusDistribution
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalPayments > 0 ? (g.Count() / (decimal)totalPayments) * 100 : 0
            })
            .ToList();

        return new AnalyticsDto
        {
            TotalRevenue = totalRevenue,
            TotalPayments = totalPayments,
            CompletedPayments = completedPayments,
            PendingPayments = pendingPayments,
            RefundedPayments = refundedPayments,
            AveragePaymentAmount = averageAmount,
            SuccessRate = successRate,
            DailyRevenue = dailyRevenue,
            StatusDistribution = statusDistribution
        };
    }
}
