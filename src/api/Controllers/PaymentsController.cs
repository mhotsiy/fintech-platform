using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/merchants/{merchantId:guid}/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new payment for a merchant
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="request">Payment creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created payment with status Pending</returns>
    /// <response code="201">Payment created successfully</response>
    /// <response code="400">Invalid request (merchant not found or validation failed)</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentDto>> CreatePayment(Guid merchantId, [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating payment for merchant {MerchantId}", merchantId);
            var payment = await _paymentService.CreatePaymentAsync(merchantId, request.AmountInMinorUnits, request.Currency.ToString(), request.ExternalReference, request.Description, cancellationToken);
            return CreatedAtAction(nameof(GetPayment), new { merchantId, paymentId = payment.Id }, payment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid payment creation request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment creation failed");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific payment by ID
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="paymentId">The unique identifier of the payment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The payment details</returns>
    /// <response code="200">Payment found</response>
    /// <response code="404">Payment not found or belongs to different merchant</response>
    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(paymentId, cancellationToken);
        
        if (payment == null || payment.MerchantId != merchantId)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    /// <summary>
    /// Get all payments for a specific merchant with optional filtering
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="dateFrom">Filter payments from this date (inclusive)</param>
    /// <param name="dateTo">Filter payments until this date (inclusive)</param>
    /// <param name="minAmount">Minimum payment amount in minor units</param>
    /// <param name="maxAmount">Maximum payment amount in minor units</param>
    /// <param name="status">Filter by status (comma-separated: Pending,Completed,Failed,Refunded)</param>
    /// <param name="search">Search in description and external reference</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payments for the merchant</returns>
    /// <response code="200">Returns the list of payments</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMerchantPayments(
        Guid merchantId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] long? minAmount,
        [FromQuery] long? maxAmount,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = new Domain.Repositories.PaymentFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            Search = search
        };

        // Parse status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            filter.Statuses = status.Split(',')
                .Select(s => Enum.TryParse<Domain.Entities.PaymentStatus>(s.Trim(), out var parsed) ? parsed : (Domain.Entities.PaymentStatus?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
        }

        var payments = await _paymentService.GetPaymentsByMerchantIdWithFilterAsync(merchantId, filter, cancellationToken);
        return Ok(payments);
    }

    /// <summary>
    /// Complete a pending payment and update merchant balance
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="paymentId">The unique identifier of the payment</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated payment with Completed status</returns>
    /// <response code="200">Payment completed successfully</response>
    /// <response code="400">Payment cannot be completed (invalid state)</response>
    /// <response code="404">Payment not found</response>
    [HttpPost("{paymentId:guid}/complete")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> CompletePayment(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Completing payment {PaymentId} for merchant {MerchantId}", paymentId, merchantId);
            var payment = await _paymentService.CompletePaymentAsync(paymentId, cancellationToken);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment completion failed");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Refund a completed payment (full or partial)
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="paymentId">The unique identifier of the payment</param>
    /// <param name="request">Refund details including optional amount and reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated payment with Refunded status</returns>
    /// <response code="200">Payment refunded successfully</response>
    /// <response code="400">Payment cannot be refunded (invalid state or insufficient balance)</response>
    /// <response code="404">Payment not found</response>
    [HttpPost("{paymentId:guid}/refund")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> RefundPayment(
        Guid merchantId, 
        Guid paymentId, 
        [FromBody] RefundPaymentRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refunding payment {PaymentId} for merchant {MerchantId}", paymentId, merchantId);
            var payment = await _paymentService.RefundPaymentAsync(
                merchantId, 
                paymentId, 
                request?.RefundAmountInMinorUnits,
                request?.Reason,
                cancellationToken);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment refund failed");
            return BadRequest(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid refund request");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Export payments to CSV with optional filtering
    /// </summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPayments(
        Guid merchantId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] long? minAmount,
        [FromQuery] long? maxAmount,
        [FromQuery] string? status,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = new Domain.Repositories.PaymentFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            Search = search
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            filter.Statuses = status.Split(',')
                .Select(s => Enum.TryParse<Domain.Entities.PaymentStatus>(s.Trim(), out var parsed) ? parsed : (Domain.Entities.PaymentStatus?)null)
                .Where(s => s.HasValue)
                .Select(s => s!.Value)
                .ToList();
        }

        var csv = await _paymentService.ExportPaymentsToCsvAsync(merchantId, filter, cancellationToken);
        var fileName = $"payments_{merchantId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    /// <summary>
    /// Create multiple payments in bulk for testing concurrency
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="request">Bulk payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of created payments</returns>
    /// <response code="201">Payments created successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<PaymentDto>>> CreateBulkPayments(
        Guid merchantId, 
        [FromBody] BulkCreatePaymentsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Payments == null || !request.Payments.Any())
            {
                return BadRequest(new { Error = "At least one payment is required" });
            }

            if (request.Payments.Count > 1000)
            {
                return BadRequest(new { Error = "Maximum 1000 payments per bulk request" });
            }

            _logger.LogInformation("Creating {Count} bulk payments for merchant {MerchantId}", 
                request.Payments.Count, merchantId);

            var payments = await _paymentService.CreateBulkPaymentsAsync(merchantId, request.Payments, cancellationToken);
            
            return CreatedAtAction(nameof(GetMerchantPayments), new { merchantId }, payments);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid bulk payment request");
            return BadRequest(new { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bulk payment creation failed");
            return BadRequest(new { Error = ex.Message });
        }
    }
}
