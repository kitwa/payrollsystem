using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Companies;
using Payroll.Domain.Audit;
using Payroll.Domain.Employees;
using Payroll.Domain.Identity;
using Payroll.Domain.Leave;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.Settings;
using Payroll.Domain.Tax;
using Payroll.Domain.Support;
using Payroll.Domain.Billing;

namespace Payroll.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<BankDetails> BankDetails => Set<BankDetails>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<Earning> Earnings => Set<Earning>();
    public DbSet<Deduction> Deductions => Set<Deduction>();
    public DbSet<EmployeeDeduction> EmployeeDeductions => Set<EmployeeDeduction>();
    public DbSet<EmployeeBonus> EmployeeBonuses => Set<EmployeeBonus>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<EarningType> EarningTypes => Set<EarningType>();
    public DbSet<DeductionType> DeductionTypes => Set<DeductionType>();
    public DbSet<TaxYear> TaxYears => Set<TaxYear>();
    public DbSet<TaxTable> TaxTables => Set<TaxTable>();
    public DbSet<TaxThreshold> TaxThresholds => Set<TaxThreshold>();
    public DbSet<TaxRebate> TaxRebates => Set<TaxRebate>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<CompanySubscription> CompanySubscriptions => Set<CompanySubscription>();
    public DbSet<CompanyPaymentProfile> CompanyPaymentProfiles => Set<CompanyPaymentProfile>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.Entity<Employee>()
            .HasIndex(e => new { e.CompanyId, e.IdNumber })
            .IsUnique()
            .HasFilter("IsDeleted = 0");
        builder.Entity<Department>()
            .HasIndex(d => new { d.CompanyId, d.Name })
            .IsUnique()
            .HasFilter("IsDeleted = 0");
    }
}
