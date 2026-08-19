namespace Payroll.Application.Reports.DTOs;

public record PayrollRegisterLineDto(
    string EmployeeNumber, string EmployeeName, decimal GrossEarnings, decimal TotalDeductions, decimal NetPay);

public record PayrollRegisterDto(Guid PeriodId, int Year, int Month, List<PayrollRegisterLineDto> Lines,
    decimal TotalGross, decimal TotalDeductions, decimal TotalNet);

public record LeaveReportLineDto(string EmployeeName, string LeaveTypeName, DateTime StartDate, DateTime EndDate, decimal Days, string Status);

public record StatutoryReportLineDto(string EmployeeNumber, string EmployeeName, decimal EmployeeAmount, decimal EmployerAmount);

public record StatutoryReportDto(Guid PeriodId, int Year, int Month, List<StatutoryReportLineDto> Lines, decimal TotalEmployee, decimal TotalEmployer);

public record EmployeeCostReportLineDto(string EmployeeNumber, string EmployeeName, decimal TotalGross, decimal TotalEmployerContributions, decimal TotalCost);

public record EmployeeCostReportDto(int Year, List<EmployeeCostReportLineDto> Lines, decimal GrandTotal);
