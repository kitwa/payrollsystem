using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Payroll.Infrastructure.Services;

public class PdfService(IAppDbContext db) : IPdfService
{
    public async Task<byte[]> GeneratePayslipAsync(Guid payrollLineId, CancellationToken ct = default)
    {
        var line = await db.PayrollLines
            .Include(l => l.Employee).ThenInclude(e => e.Company)
            .Include(l => l.PayrollPeriod)
            .Include(l => l.Earnings)
            .Include(l => l.Deductions)
            .FirstOrDefaultAsync(l => l.Id == payrollLineId, ct)
            ?? throw new InvalidOperationException("Payroll line not found.");

        var employee = line.Employee;
        var company = employee.Company;
        var period = line.PayrollPeriod;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    if (company.LogoData is { Length: > 0 } && company.LogoContentType != "image/svg+xml")
                    {
                        col.Item().Height(45).AlignLeft().Image(company.LogoData).FitHeight();
                    }
                    col.Item().Text(company.Name).FontSize(16).Bold();
                    col.Item().Text($"Payslip for {period.Year}/{period.Month:00}").FontSize(12);
                    col.Item().PaddingTop(5).LineHorizontal(1);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Employee: {employee.FirstName} {employee.LastName}");
                            c.Item().Text($"Employee Number: {employee.EmployeeNumber}");
                            c.Item().Text($"Job Title: {employee.JobTitle}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Tax Number: {employee.TaxNumber}");
                            c.Item().Text($"UIF Number: {employee.UifNumber}");
                            c.Item().Text($"Pay Period: {period.PeriodStart:d} - {period.PeriodEnd:d}");
                        });
                    });

                    col.Item().PaddingTop(10).Text("Earnings").Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                        table.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().AlignRight().Text("Amount").Bold();
                        });
                        foreach (var e in line.Earnings)
                        {
                            table.Cell().Text(e.Description);
                            table.Cell().AlignRight().Text($"R {e.Amount:N2}");
                        }
                    });

                    col.Item().PaddingTop(10).Text("Deductions").Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(1); });
                        table.Header(h =>
                        {
                            h.Cell().Text("Description").Bold();
                            h.Cell().AlignRight().Text("Amount").Bold();
                        });
                        foreach (var d in line.Deductions.Where(d => d.EmployeeAmount > 0))
                        {
                            table.Cell().Text(d.Description);
                            table.Cell().AlignRight().Text($"R {d.EmployeeAmount:N2}");
                        }
                    });

                    col.Item().PaddingTop(15).LineHorizontal(1);
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Taxable Income");
                        row.RelativeItem().AlignRight().Text($"R {line.TaxableIncome:N2}");
                    });
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Gross Earnings").Bold();
                        row.RelativeItem().AlignRight().Text($"R {line.GrossEarnings:N2}");
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total Deductions").Bold();
                        row.RelativeItem().AlignRight().Text($"R {line.TotalDeductions:N2}");
                    });
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().Text("Net Pay").FontSize(13).Bold();
                        row.RelativeItem().AlignRight().Text($"R {line.NetPay:N2}").FontSize(13).Bold();
                    });
                });

                page.Footer().AlignCenter().Text("Generated by Payroll SA").FontSize(8);
            });
        });

        return document.GeneratePdf();
    }
}
