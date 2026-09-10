namespace Payroll.Application.Common.Interfaces;

public record PaymentCustomerResult(string CustomerReference);

public record CheckoutSessionResult(string SessionReference, string SubscriptionReference, string CheckoutUrl, bool CompletedImmediately);

/// <summary>
/// Abstraction over the payment provider so Payroll SA's subscription logic never depends on a
/// specific vendor SDK. Implement this once a provider (e.g. Paystack, PayFast, Stripe) is selected.
/// </summary>
public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<PaymentCustomerResult> CreateCustomerAsync(Guid companyId, string companyName, string billingEmail, CancellationToken ct = default);
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(string customerReference, string planCode, decimal monthlyPrice, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string subscriptionReference, CancellationToken ct = default);
}
