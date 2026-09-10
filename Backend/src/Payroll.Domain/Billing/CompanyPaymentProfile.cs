using Payroll.Domain.Common;

namespace Payroll.Domain.Billing;

/// <summary>
/// Safe, non-sensitive billing references only. Never store card numbers, CVV, full bank account
/// numbers, online banking credentials, or PINs here — only provider tokens/references.
/// </summary>
public class CompanyPaymentProfile : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? AccountType { get; set; }
    public string? AccountLast4 { get; set; }
    public string? PaymentProvider { get; set; }
    public string? PaymentCustomerReference { get; set; }
    public string? PaymentMethodReference { get; set; }
    public string? MandateReference { get; set; }
    public string? CardBrand { get; set; }
}
