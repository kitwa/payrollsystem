using System.Net;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payroll.Application.Common;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Domain.Support;
using Payroll.Shared;

namespace Payroll.Application.Support;

public record SupportTicketListDto(
    Guid Id,
    string TicketNumber,
    Guid CompanyId,
    string CompanyName,
    Guid CreatedByUserId,
    string CreatedByName,
    string Subject,
    SupportTicketType Type,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? ClosedAt);

public record SupportTicketDetailDto(
    Guid Id,
    string TicketNumber,
    Guid CompanyId,
    string CompanyName,
    Guid CreatedByUserId,
    string CreatedByName,
    string CreatedByEmail,
    string Subject,
    SupportTicketType Type,
    string Description,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? ClosedAt,
    Guid? ClosedByUserId);

public record GetSupportTicketsQuery(
    Guid? CompanyId,
    SupportTicketStatus? Status,
    SupportTicketType? Type,
    string? Search,
    PaginationParams Params) : IRequest<Result<PagedList<SupportTicketListDto>>>;

public class GetSupportTicketsHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<GetSupportTicketsQuery, Result<PagedList<SupportTicketListDto>>>
{
    public async Task<Result<PagedList<SupportTicketListDto>>> Handle(GetSupportTicketsQuery request, CancellationToken ct)
    {
        var query = db.SupportTickets.Where(ticket => !ticket.IsDeleted);
        if (currentUser.IsInRole(Constants.Roles.SuperAdmin))
        {
            if (request.CompanyId is not null)
                query = query.Where(ticket => ticket.CompanyId == request.CompanyId);
        }
        else
        {
            if (currentUser.CompanyId is null)
                return Result<PagedList<SupportTicketListDto>>.Fail("Your account is not linked to a company.");
            query = query.Where(ticket => ticket.CompanyId == currentUser.CompanyId.Value);
        }

        if (request.Status is not null) query = query.Where(ticket => ticket.Status == request.Status);
        if (request.Type is not null) query = query.Where(ticket => ticket.Type == request.Type);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            var matchingCompanyIds = await db.Companies
                .Where(company => company.Name.ToLower().Contains(search))
                .Select(company => company.Id)
                .ToListAsync(ct);
            query = query.Where(ticket => ticket.TicketNumber.ToLower().Contains(search)
                || ticket.Subject.ToLower().Contains(search)
                || matchingCompanyIds.Contains(ticket.CompanyId));
        }

        var rows = await query
            .Join(db.Companies, ticket => ticket.CompanyId, company => company.Id,
                (ticket, company) => new { ticket, company.Name })
            .OrderByDescending(row => row.ticket.CreatedAt)
            .ToListAsync(ct);

        var users = await userManager.Users
            .Where(user => rows.Select(row => row.ticket.CreatedByUserId).Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, ct);

        var items = rows.Select(row => new SupportTicketListDto(
            row.ticket.Id,
            row.ticket.TicketNumber,
            row.ticket.CompanyId,
            row.Name,
            row.ticket.CreatedByUserId,
            users.TryGetValue(row.ticket.CreatedByUserId, out var user) ? $"{user.FirstName} {user.LastName}".Trim() : "Unknown user",
            row.ticket.Subject,
            row.ticket.Type,
            row.ticket.Status,
            row.ticket.CreatedAt,
            row.ticket.ModifiedAt,
            row.ticket.ClosedAt));

        return Result<PagedList<SupportTicketListDto>>.Ok(
            PagedList<SupportTicketListDto>.Create(items, request.Params.PageNumber, request.Params.PageSize));
    }
}

public record GetSupportTicketQuery(Guid Id) : IRequest<Result<SupportTicketDetailDto>>;

public class GetSupportTicketHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser) : IRequestHandler<GetSupportTicketQuery, Result<SupportTicketDetailDto>>
{
    public async Task<Result<SupportTicketDetailDto>> Handle(GetSupportTicketQuery request, CancellationToken ct)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, ct);
        if (ticket is null) return Result<SupportTicketDetailDto>.Fail("Support ticket not found.");
        if (!CanAccess(ticket)) return Result<SupportTicketDetailDto>.Fail("You are not authorized to view this ticket.");

        var company = await db.Companies.FirstOrDefaultAsync(item => item.Id == ticket.CompanyId, ct);
        var user = await userManager.FindByIdAsync(ticket.CreatedByUserId.ToString());
        return Result<SupportTicketDetailDto>.Ok(new SupportTicketDetailDto(
            ticket.Id,
            ticket.TicketNumber,
            ticket.CompanyId,
            company?.Name ?? "Unknown company",
            ticket.CreatedByUserId,
            user is null ? "Unknown user" : $"{user.FirstName} {user.LastName}".Trim(),
            user?.Email ?? string.Empty,
            ticket.Subject,
            ticket.Type,
            ticket.Description,
            ticket.Status,
            ticket.CreatedAt,
            ticket.ModifiedAt,
            ticket.ClosedAt,
            ticket.ClosedByUserId));
    }

    private bool CanAccess(SupportTicket ticket) => currentUser.IsInRole(Constants.Roles.SuperAdmin)
        || currentUser.CompanyId == ticket.CompanyId;
}

public record CreateSupportTicketDto(string Subject, SupportTicketType Type, string Description);
public record CreateSupportTicketCommand(CreateSupportTicketDto Dto) : IRequest<Result<SupportTicketDetailDto>>;

public class CreateSupportTicketHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IEmailService emailService,
    ISupportNotificationSettings notificationSettings,
    ILogger<CreateSupportTicketHandler> logger) : IRequestHandler<CreateSupportTicketCommand, Result<SupportTicketDetailDto>>
{
    public async Task<Result<SupportTicketDetailDto>> Handle(CreateSupportTicketCommand request, CancellationToken ct)
    {
        if (currentUser.CompanyId is null || currentUser.UserId == Guid.Empty)
            return Result<SupportTicketDetailDto>.Fail("Your account is not linked to a company.");
        if (string.IsNullOrWhiteSpace(request.Dto.Subject))
            return Result<SupportTicketDetailDto>.Fail("Ticket subject is required.");
        if (string.IsNullOrWhiteSpace(request.Dto.Description))
            return Result<SupportTicketDetailDto>.Fail("Ticket description is required.");

        var company = await db.Companies.FirstOrDefaultAsync(item => item.Id == currentUser.CompanyId.Value && !item.IsDeleted, ct);
        if (company is null) return Result<SupportTicketDetailDto>.Fail("Company not found.");

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString());
        var ticket = new SupportTicket
        {
            TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CompanyId = company.Id,
            CreatedByUserId = currentUser.UserId,
            Subject = request.Dto.Subject.Trim(),
            Description = request.Dto.Description.Trim(),
            Type = request.Dto.Type,
            Status = SupportTicketStatus.Open,
            CreatedBy = currentUser.UserId.ToString()
        };

        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        var detail = new SupportTicketDetailDto(
            ticket.Id, ticket.TicketNumber, company.Id, company.Name, currentUser.UserId,
            user is null ? "Unknown user" : $"{user.FirstName} {user.LastName}".Trim(),
            user?.Email ?? currentUser.Email, ticket.Subject, ticket.Type, ticket.Description,
            ticket.Status, ticket.CreatedAt, ticket.ModifiedAt, ticket.ClosedAt, ticket.ClosedByUserId);

        await SendNotificationsAsync(detail, ct);
        return Result<SupportTicketDetailDto>.Ok(detail);
    }

    private async Task SendNotificationsAsync(SupportTicketDetailDto ticket, CancellationToken ct) =>
        await SupportTicketNotifier.NotifyCreatedAsync(emailService, userManager, notificationSettings, logger, ticket, ct);
}

public record CloseSupportTicketCommand(Guid Id) : IRequest<Result<SupportTicketDetailDto>>;

public class CloseSupportTicketHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IEmailService emailService,
    ISupportNotificationSettings notificationSettings,
    ILogger<CloseSupportTicketHandler> logger) : IRequestHandler<CloseSupportTicketCommand, Result<SupportTicketDetailDto>>
{
    public async Task<Result<SupportTicketDetailDto>> Handle(CloseSupportTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, ct);
        if (ticket is null) return Result<SupportTicketDetailDto>.Fail("Support ticket not found.");
        if (currentUser.IsInRole(Constants.Roles.SuperAdmin) || currentUser.CompanyId != ticket.CompanyId || ticket.CreatedByUserId != currentUser.UserId)
            return Result<SupportTicketDetailDto>.Fail("You are not authorized to close this ticket.");
        if (ticket.Status == SupportTicketStatus.Closed)
            return Result<SupportTicketDetailDto>.Fail("This ticket is already closed.");

        ticket.Status = SupportTicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.ClosedByUserId = currentUser.UserId;
        ticket.ModifiedAt = DateTime.UtcNow;
        ticket.ModifiedBy = currentUser.UserId.ToString();
        await db.SaveChangesAsync(ct);

        var user = await userManager.FindByIdAsync(ticket.CreatedByUserId.ToString());
        var company = await db.Companies.FirstOrDefaultAsync(item => item.Id == ticket.CompanyId, ct);
        var detail = new SupportTicketDetailDto(ticket.Id, ticket.TicketNumber, ticket.CompanyId, company?.Name ?? "Unknown company",
            ticket.CreatedByUserId, user is null ? "Unknown user" : $"{user.FirstName} {user.LastName}".Trim(), user?.Email ?? string.Empty,
            ticket.Subject, ticket.Type, ticket.Description, ticket.Status, ticket.CreatedAt, ticket.ModifiedAt, ticket.ClosedAt, ticket.ClosedByUserId);
        await SupportTicketNotifier.NotifyStatusChangedAsync(emailService, userManager, notificationSettings, logger, detail, ct);
        return Result<SupportTicketDetailDto>.Ok(detail);
    }
}

public record UpdateSupportTicketStatusCommand(Guid Id, SupportTicketStatus Status) : IRequest<Result<SupportTicketDetailDto>>;

public class UpdateSupportTicketStatusHandler(
    IAppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUser currentUser,
    IEmailService emailService,
    ISupportNotificationSettings notificationSettings,
    ILogger<UpdateSupportTicketStatusHandler> logger) : IRequestHandler<UpdateSupportTicketStatusCommand, Result<SupportTicketDetailDto>>
{
    public async Task<Result<SupportTicketDetailDto>> Handle(UpdateSupportTicketStatusCommand request, CancellationToken ct)
    {
        if (!currentUser.IsInRole(Constants.Roles.SuperAdmin))
            return Result<SupportTicketDetailDto>.Fail("Only Super Admin users can manage ticket status.");

        var ticket = await db.SupportTickets.FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, ct);
        if (ticket is null) return Result<SupportTicketDetailDto>.Fail("Support ticket not found.");
        var previousStatus = ticket.Status;
        ticket.Status = request.Status;
        ticket.ModifiedAt = DateTime.UtcNow;
        ticket.ModifiedBy = currentUser.UserId.ToString();
        if (request.Status == SupportTicketStatus.Closed)
        {
            ticket.ClosedAt ??= DateTime.UtcNow;
            ticket.ClosedByUserId = currentUser.UserId;
        }
        else
        {
            ticket.ClosedAt = null;
            ticket.ClosedByUserId = null;
        }
        await db.SaveChangesAsync(ct);

        var user = await userManager.FindByIdAsync(ticket.CreatedByUserId.ToString());
        var company = await db.Companies.FirstOrDefaultAsync(item => item.Id == ticket.CompanyId, ct);
        var detail = new SupportTicketDetailDto(ticket.Id, ticket.TicketNumber, ticket.CompanyId, company?.Name ?? "Unknown company",
            ticket.CreatedByUserId, user is null ? "Unknown user" : $"{user.FirstName} {user.LastName}".Trim(), user?.Email ?? string.Empty,
            ticket.Subject, ticket.Type, ticket.Description, ticket.Status, ticket.CreatedAt, ticket.ModifiedAt, ticket.ClosedAt, ticket.ClosedByUserId);
        if (previousStatus != request.Status) await SupportTicketNotifier.NotifyStatusChangedAsync(emailService, userManager, notificationSettings, logger, detail, ct);
        return Result<SupportTicketDetailDto>.Ok(detail);
    }
}

/// <summary>Shared support-ticket notification emails, used by create/close/status-change flows alike.</summary>
file static class SupportTicketNotifier
{
    public static async Task NotifyCreatedAsync(
        IEmailService emailService, UserManager<AppUser> userManager, ISupportNotificationSettings notificationSettings,
        ILogger logger, SupportTicketDetailDto ticket, CancellationToken ct)
    {
        try
        {
            var link = BuildLink(notificationSettings, ticket.Id);
            var body = $"""
                <p>A new support ticket has been submitted.</p>
                <p><strong>Ticket:</strong> #{WebUtility.HtmlEncode(ticket.TicketNumber)}<br/>
                <strong>Company:</strong> {WebUtility.HtmlEncode(ticket.CompanyName)}<br/>
                <strong>Submitted by:</strong> {WebUtility.HtmlEncode(ticket.CreatedByName)}<br/>
                <strong>Type:</strong> {WebUtility.HtmlEncode(ticket.Type.ToString())}<br/>
                <strong>Subject:</strong> {WebUtility.HtmlEncode(ticket.Subject)}</p>
                <p>{WebUtility.HtmlEncode(ticket.Description).Replace("\n", "<br/>")}</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">View ticket</a></p>
                """;

            if (!string.IsNullOrWhiteSpace(ticket.CreatedByEmail))
                await emailService.SendAsync(ticket.CreatedByEmail, $"Support Ticket #{ticket.TicketNumber} Received",
                    $"<p>Your support ticket has been received and is currently being reviewed.</p>{body}", ct);

            await NotifySuperAdminsAsync(emailService, userManager, notificationSettings, $"New Support Ticket #{ticket.TicketNumber}", body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Support ticket {TicketNumber} creation notification failed.", ticket.TicketNumber);
        }
    }

    public static async Task NotifyStatusChangedAsync(
        IEmailService emailService, UserManager<AppUser> userManager, ISupportNotificationSettings notificationSettings,
        ILogger logger, SupportTicketDetailDto ticket, CancellationToken ct)
    {
        try
        {
            var link = BuildLink(notificationSettings, ticket.Id);
            var subject = $"Support Ticket #{ticket.TicketNumber}: {ticket.Status}";
            var body = $"""
                <p>{StatusMessage(ticket.Status)}</p>
                <p><strong>Ticket:</strong> #{WebUtility.HtmlEncode(ticket.TicketNumber)}<br/>
                <strong>Subject:</strong> {WebUtility.HtmlEncode(ticket.Subject)}<br/>
                <strong>Status:</strong> {WebUtility.HtmlEncode(ticket.Status.ToString())}</p>
                <p><a href="{WebUtility.HtmlEncode(link)}">View ticket</a></p>
                """;

            if (!string.IsNullOrWhiteSpace(ticket.CreatedByEmail))
                await emailService.SendAsync(ticket.CreatedByEmail, subject, body, ct);

            await NotifySuperAdminsAsync(emailService, userManager, notificationSettings, subject, body, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Support ticket {TicketNumber} status notification failed.", ticket.TicketNumber);
        }
    }

    private static async Task NotifySuperAdminsAsync(
        IEmailService emailService, UserManager<AppUser> userManager, ISupportNotificationSettings notificationSettings,
        string subject, string body, CancellationToken ct)
    {
        var superAdmins = await userManager.GetUsersInRoleAsync(Constants.Roles.SuperAdmin);
        var notified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var admin in superAdmins.Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.Email)))
        {
            await emailService.SendAsync(admin.Email!, subject, body, ct);
            notified.Add(admin.Email!);
        }

        if (!string.IsNullOrWhiteSpace(notificationSettings.SupportEmail) && !notified.Contains(notificationSettings.SupportEmail))
            await emailService.SendAsync(notificationSettings.SupportEmail, subject, body, ct);
    }

    private static string BuildLink(ISupportNotificationSettings notificationSettings, Guid ticketId) =>
        $"{notificationSettings.FrontendBaseUrl.TrimEnd('/')}/support/tickets/{ticketId}";

    private static string StatusMessage(SupportTicketStatus status) => status switch
    {
        SupportTicketStatus.Open => "Your support ticket is open and will be reviewed by our team shortly.",
        SupportTicketStatus.InReview => "Your support ticket is currently being reviewed by our support team.",
        SupportTicketStatus.InProgress => "Our team is actively working on your support ticket.",
        SupportTicketStatus.Resolved => "Your support ticket has been marked as resolved. Please let us know if you need further help.",
        SupportTicketStatus.Closed => "Your support ticket has been closed.",
        _ => "Your support ticket status has been updated."
    };
}
