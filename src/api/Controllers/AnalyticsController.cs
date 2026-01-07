using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/merchants/{merchantId:guid}/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get analytics dashboard data for a merchant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AnalyticsDto>> GetAnalytics(
        Guid merchantId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var analytics = await _analyticsService.GetMerchantAnalyticsAsync(
            merchantId, 
            fromDate ?? DateTime.UtcNow.AddMonths(-1),
            toDate ?? DateTime.UtcNow,
            cancellationToken);

        return Ok(analytics);
    }
}

public class AnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalPayments { get; set; }
    public int CompletedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int RefundedPayments { get; set; }
    public decimal AveragePaymentAmount { get; set; }
    public decimal SuccessRate { get; set; }
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<PaymentStatusDistribution> StatusDistribution { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int Count { get; set; }
}

public class PaymentStatusDistribution
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
