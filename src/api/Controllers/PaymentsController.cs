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
    /// Get all payments for a specific merchant
    /// </summary>
    /// <param name="merchantId">The unique identifier of the merchant</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payments for the merchant</returns>
    /// <response code="200">Returns the list of payments</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMerchantPayments(Guid merchantId, CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetPaymentsByMerchantIdAsync(merchantId, cancellationToken);
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
}
