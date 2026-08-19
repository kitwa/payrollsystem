namespace Payroll.Application.Tax.DTOs;

public record TaxTableDto(Guid Id, decimal IncomeFrom, decimal IncomeTo, decimal BaseTax, decimal MarginalRate);
public record TaxThresholdDto(Guid Id, string AgeGroup, decimal ThresholdAmount);
public record TaxRebateDto(Guid Id, string RebateType, decimal Amount);

public record TaxYearSummaryDto(Guid Id, int Year, DateTime StartDate, DateTime EndDate, bool IsActive);

public record TaxYearDetailDto(
    Guid Id,
    int Year,
    DateTime StartDate,
    DateTime EndDate,
    decimal UifMonthlyEarningsCeiling,
    decimal UifContributionRate,
    decimal SdlRate,
    bool IsActive,
    List<TaxTableDto> TaxTables,
    List<TaxThresholdDto> TaxThresholds,
    List<TaxRebateDto> TaxRebates);

public record UpdateTaxTableDto(decimal IncomeFrom, decimal IncomeTo, decimal BaseTax, decimal MarginalRate);
public record UpdateTaxThresholdDto(decimal ThresholdAmount);
public record UpdateTaxRebateDto(decimal Amount);
