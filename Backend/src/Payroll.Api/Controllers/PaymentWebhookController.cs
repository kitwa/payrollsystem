using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Billing.Commands;
using Payroll.Application.Billing.DTOs;

namespace Payroll.Api.Controllers;

/// <summary>
/// Receives trusted payment-provider events. Payment/subscription status must never be trusted from
/// the Angular app — this endpoint is the only way a subscription becomes ACTIVE from a real payment.
/// Verified using a shared secret header until a real provider's own signature verification replaces it.
/// </summary>
[AllowAnonymous]
[Route("api/payment/webhook")]
public class PaymentWebhookController(IMediator mediator, IConfiguration configuration) : BaseApiController(mediator)
{
    [HttpPost]
    public async Task<IActionResult> Handle([FromBody] WebhookEventDto dto, CancellationToken ct)
    {
        var expectedSecret = configuration["PaymentSettings:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(expectedSecret))
        {
            if (!Request.Headers.TryGetValue("X-Webhook-Secret", out var provided) || provided != expectedSecret)
                return Unauthorized();
        }

        return FromResult(await Mediator.Send(new ProcessPaymentWebhookCommand(dto), ct));
    }
}
