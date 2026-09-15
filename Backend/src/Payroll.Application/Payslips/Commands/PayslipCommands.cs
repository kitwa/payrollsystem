using MediatR;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Common.Interfaces;
using Payroll.Application.Payslips.DTOs;
using Payroll.Shared;

namespace Payroll.Application.Payslips.Commands;

public record EmailPayslipCommand(Guid PayrollLineId) : IRequest<Result<EmailPayslipResultDto>>;

public class EmailPayslipHandler(
    IAppDbContext db,
    IPdfService pdfService,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<EmailPayslipCommand, Result<EmailPayslipResultDto>>
{
    public async Task<Result<EmailPayslipResultDto>> Handle(EmailPayslipCommand request, CancellationToken ct)
    {
        var line = await db.PayrollLines
            .Include(l => l.Employee)
            .Include(l => l.PayrollPeriod)
            .FirstOrDefaultAsync(l => l.Id == request.PayrollLineId && !l.IsDeleted, ct);

        if (line is null) return Result<EmailPayslipResultDto>.Fail("Payslip not found.");
        if (!PayslipAccess.CanManageCompany(currentUser, line.Employee.CompanyId))
            return Result<EmailPayslipResultDto>.Fail("You are not authorized to email this payslip.");

        if (string.IsNullOrWhiteSpace(line.Employee.Email))
            return Result<EmailPayslipResultDto>.Fail("This employee has no email address on record.");

        var pdf = await pdfService.GeneratePayslipAsync(line.Id, ct);
        var fileName = $"payslip-{line.PayrollPeriod.Year}-{line.PayrollPeriod.Month:00}.pdf";

        try
        {
            await emailService.SendPayslipAsync(
                line.Employee.Email,
                $"{line.Employee.FirstName} {line.Employee.LastName}",
                pdf, fileName, EmailSenderType.Billing, ct);
        }
        catch (Exception ex)
        {
            return Result<EmailPayslipResultDto>.Fail($"Could not send the payslip: {ex.Message}");
        }

        line.PayslipEmailedAt = DateTime.UtcNow;
        line.PayslipEmailedTo = line.Employee.Email;
        await db.SaveChangesAsync(ct);

        return Result<EmailPayslipResultDto>.Ok(new EmailPayslipResultDto(1, 0, []));
    }
}

public record EmailPeriodPayslipsCommand(Guid PeriodId) : IRequest<Result<EmailPayslipResultDto>>;

public class EmailPeriodPayslipsHandler(
    IAppDbContext db,
    IPdfService pdfService,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<EmailPeriodPayslipsCommand, Result<EmailPayslipResultDto>>
{
    public async Task<Result<EmailPayslipResultDto>> Handle(EmailPeriodPayslipsCommand request, CancellationToken ct)
    {
        var period = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId && !p.IsDeleted, ct);
        if (period is null) return Result<EmailPayslipResultDto>.Fail("Payroll period not found.");

        if (!PayslipAccess.CanManageCompany(currentUser, period.CompanyId))
            return Result<EmailPayslipResultDto>.Fail("You are not authorized to email these payslips.");

        var lines = await db.PayrollLines
            .Include(l => l.Employee)
            .Where(l => l.PayrollPeriodId == request.PeriodId && !l.IsDeleted)
            .ToListAsync(ct);

        if (lines.Count == 0) return Result<EmailPayslipResultDto>.Fail("This payroll period has no payslips.");

        var sent = 0;
        var skipped = 0;
        var failures = new List<string>();

        foreach (var line in lines)
        {
            var employeeName = $"{line.Employee.FirstName} {line.Employee.LastName}";

            if (string.IsNullOrWhiteSpace(line.Employee.Email))
            {
                skipped++;
                failures.Add($"{employeeName}: no email address on record.");
                continue;
            }

            try
            {
                var pdf = await pdfService.GeneratePayslipAsync(line.Id, ct);
                await emailService.SendPayslipAsync(
                    line.Employee.Email, employeeName, pdf,
                    $"payslip-{period.Year}-{period.Month:00}.pdf", EmailSenderType.Billing, ct);

                line.PayslipEmailedAt = DateTime.UtcNow;
                line.PayslipEmailedTo = line.Employee.Email;
                sent++;
            }
            catch (Exception ex)
            {
                failures.Add($"{employeeName}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);
        return Result<EmailPayslipResultDto>.Ok(new EmailPayslipResultDto(sent, skipped, failures));
    }
}
