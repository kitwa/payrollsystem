using Payroll.Application.Common.Interfaces;

namespace Payroll.Infrastructure.Services;

/// <summary>
/// Placeholder payment gateway used until a real provider (e.g. Paystack, PayFast, Stripe) is
/// configured. It never talks to a real payment network and never stores card/bank credentials.
/// It confirms checkout synchronously purely so the upgrade flow can be exercised end-to-end during
/// development. Replace this with a real IPaymentGateway implementation and update
/// DependencyInjection.AddInfrastructure to register it once a provider is selected.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    public string ProviderName => "Mock";

    public Task<PaymentCustomerResult> CreateCustomerAsync(Guid companyId, string companyName, string billingEmail, CancellationToken ct = default) =>
        Task.FromResult(new PaymentCustomerResult($"mock_cust_{companyId:N}"));

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(string customerReference, string planCode, decimal monthlyPrice, CancellationToken ct = default) =>
        Task.FromResult(new CheckoutSessionResult(
            SessionReference: $"mock_session_{Guid.NewGuid():N}",
            SubscriptionReference: $"mock_sub_{Guid.NewGuid():N}",
            CheckoutUrl: string.Empty,
            CompletedImmediately: true));

    public Task CancelSubscriptionAsync(string subscriptionReference, CancellationToken ct = default) => Task.CompletedTask;
}
