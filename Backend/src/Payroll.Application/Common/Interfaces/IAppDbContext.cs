using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Companies;
using Payroll.Domain.Audit;
using Payroll.Domain.Employees;
using Payroll.Domain.Leave;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.Settings;
using Payroll.Domain.Tax;

namespace Payroll.Application.Common.Interfaces;

/// <summary>Abstraction over EF Core — allows handlers to be tested without a real database.</summary>
public interface IAppDbContext
{
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Company> Companies { get; }
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<BankDetails> BankDetails { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<PayrollPeriod> PayrollPeriods { get; }
    DbSet<PayrollLine> PayrollLines { get; }
    DbSet<Earning> Earnings { get; }
    DbSet<Deduction> Deductions { get; }
    DbSet<EmployeeDeduction> EmployeeDeductions { get; }
    DbSet<EmployeeBonus> EmployeeBonuses { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<EarningType> EarningTypes { get; }
    DbSet<DeductionType> DeductionTypes { get; }
    DbSet<TaxYear> TaxYears { get; }
    DbSet<TaxTable> TaxTables { get; }
    DbSet<TaxThreshold> TaxThresholds { get; }
    DbSet<TaxRebate> TaxRebates { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
