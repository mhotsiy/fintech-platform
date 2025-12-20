using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FintechPlatform.Api.Controllers;

[ApiController]
[Route("api/merchants/{merchantId:guid}/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> CreatePayment(Guid merchantId, [FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating payment for merchant {MerchantId}", merchantId);
            var payment = await _paymentService.CreatePaymentAsync(merchantId, request.AmountInMinorUnits, request.Currency, request.ExternalReference, request.Description, cancellationToken);
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

    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<PaymentDto>> GetPayment(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(paymentId, cancellationToken);
        
        if (payment == null || payment.MerchantId != merchantId)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetMerchantPayments(Guid merchantId, CancellationToken cancellationToken)
    {
        var payments = await _paymentService.GetPaymentsByMerchantIdAsync(merchantId, cancellationToken);
        return Ok(payments);
    }

    [HttpPost("{paymentId:guid}/complete")]
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
