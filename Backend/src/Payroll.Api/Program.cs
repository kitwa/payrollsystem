using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Payroll.Infrastructure;
using Payroll.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Payroll.Application.Auth.Commands.LoginCommand).Assembly);
        cfg.AddOpenBehavior(typeof(Payroll.Application.Common.Behaviours.ValidationBehaviour<,>));
        cfg.AddOpenBehavior(typeof(Payroll.Application.Common.Behaviours.LoggingBehaviour<,>));
    });

    builder.Services.AddControllers();
    builder.Services.AddCors(opt => opt.AddPolicy("CorsPolicy", p =>
        p.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://localhost:4200", "https://localhost:4200")));

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payroll SA API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT. Example: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // Auto-migrate and seed on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Payroll.Domain.Identity.AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Payroll.Domain.Identity.AppRole>>();
        await db.Database.MigrateAsync();
        await EnsureAuditTableAsync(db);
        await EnsureEmployeeDeductionsTableAsync(db);
        await EnsureCompanyLogoColumnsAsync(db);
        await EnsureDepartmentSystemFlagColumnAsync(db);
        await EnsureCompanyPayrollSettingsColumnsAsync(db);
        await EnsureEmployeeBonusesTableAsync(db);
        await EnsureSupportTicketsTableAsync(db);
        await EnsurePayrollLineTaxableIncomeColumnAsync(db);
        await EnsureCompanySubscriptionsTableAsync(db);
        await EnsureCompanyPaymentProfilesTableAsync(db);
        await EnsurePaymentWebhookEventsTableAsync(db);
        await EnsureDefaultDepartmentsAsync(db);
        await EnsureDefaultSubscriptionsAsync(db);
        await Seed.SeedAsync(db, userManager, roleManager);
    }

    // First month free, all features included, capped at 5 employees — swept daily so the free
    // trial expires even if nobody makes a request that day.
    using (var scope = app.Services.CreateScope())
    {
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<Payroll.Application.Common.Interfaces.ISubscriptionService>(
            "expire-overdue-trials", svc => svc.ExpireOverdueTrialsAsync(CancellationToken.None), Cron.Daily);
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<Payroll.Api.Middleware.ExceptionMiddleware>();
    app.UseMiddleware<Payroll.Api.Middleware.AuditMiddleware>();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("CorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<Payroll.Api.Middleware.CompanyStatusMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payroll SA v1"));
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new Payroll.Api.Middleware.HangfireAuthFilter()]
    });
    app.MapControllers();
    app.MapFallbackToFile("index.html");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static async Task EnsureAuditTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "AuditLogs" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_AuditLogs" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "CompanyId" TEXT NULL,
            "UserId" TEXT NULL,
            "UserEmail" TEXT NULL,
            "IpAddress" TEXT NULL,
            "HttpMethod" TEXT NOT NULL,
            "Path" TEXT NOT NULL,
            "StatusCode" INTEGER NOT NULL,
            "Action" TEXT NULL,
            "OccurredAt" TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CompanyId_OccurredAt"
            ON "AuditLogs" ("CompanyId", "OccurredAt");
        CREATE INDEX IF NOT EXISTS "IX_AuditLogs_OccurredAt"
            ON "AuditLogs" ("OccurredAt");
        """);
}

static async Task EnsureEmployeeDeductionsTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "EmployeeDeductions" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_EmployeeDeductions" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "CompanyId" TEXT NOT NULL,
            "EmployeeId" TEXT NOT NULL,
            "Category" INTEGER NOT NULL,
            "Description" TEXT NOT NULL,
            "EmployeeAmount" TEXT NOT NULL,
            "EmployerAmount" TEXT NOT NULL,
            "IsActive" INTEGER NOT NULL,
            CONSTRAINT "FK_EmployeeDeductions_Employees_EmployeeId"
                FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_EmployeeDeductions_EmployeeId"
            ON "EmployeeDeductions" ("EmployeeId");
        CREATE INDEX IF NOT EXISTS "IX_EmployeeDeductions_CompanyId_EmployeeId"
            ON "EmployeeDeductions" ("CompanyId", "EmployeeId");
        """);

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('EmployeeDeductions')").ToListAsync();
    if (!columns.Contains("DeductionTypeId"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "EmployeeDeductions" ADD COLUMN "DeductionTypeId" TEXT NULL;""");
    await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_EmployeeDeductions_DeductionTypeId" ON "EmployeeDeductions" ("DeductionTypeId");""");
}

static async Task EnsureCompanyLogoColumnsAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Companies')").ToListAsync();
    if (!columns.Contains("LogoData"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Companies" ADD COLUMN "LogoData" BLOB NULL;""");
    if (!columns.Contains("LogoContentType"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Companies" ADD COLUMN "LogoContentType" TEXT NULL;""");
}

static async Task EnsureDepartmentSystemFlagColumnAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Departments')").ToListAsync();
    if (!columns.Contains("IsSystemDepartment"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Departments" ADD COLUMN "IsSystemDepartment" INTEGER NOT NULL DEFAULT 0;""");
}

static async Task EnsureCompanyPayrollSettingsColumnsAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('Companies')").ToListAsync();
    if (!columns.Contains("IsUifEnabled"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Companies" ADD COLUMN "IsUifEnabled" INTEGER NOT NULL DEFAULT 1;""");
    if (!columns.Contains("IsSdlEnabled"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "Companies" ADD COLUMN "IsSdlEnabled" INTEGER NOT NULL DEFAULT 1;""");
}

static async Task EnsureEmployeeBonusesTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "EmployeeBonuses" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_EmployeeBonuses" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "CompanyId" TEXT NOT NULL,
            "EmployeeId" TEXT NOT NULL,
            "PayrollPeriodId" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "Amount" TEXT NOT NULL,
            "Notes" TEXT NULL,
            CONSTRAINT "FK_EmployeeBonuses_Employees_EmployeeId"
                FOREIGN KEY ("EmployeeId") REFERENCES "Employees" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_EmployeeBonuses_PayrollPeriods_PayrollPeriodId"
                FOREIGN KEY ("PayrollPeriodId") REFERENCES "PayrollPeriods" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS "IX_EmployeeBonuses_EmployeeId"
            ON "EmployeeBonuses" ("EmployeeId");
        CREATE INDEX IF NOT EXISTS "IX_EmployeeBonuses_PayrollPeriodId"
            ON "EmployeeBonuses" ("PayrollPeriodId");
        CREATE INDEX IF NOT EXISTS "IX_EmployeeBonuses_CompanyId_EmployeeId_PayrollPeriodId"
            ON "EmployeeBonuses" ("CompanyId", "EmployeeId", "PayrollPeriodId");
        """);

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('EmployeeBonuses')").ToListAsync();
    if (!columns.Contains("EarningTypeId"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "EmployeeBonuses" ADD COLUMN "EarningTypeId" TEXT NULL;""");
    await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_EmployeeBonuses_EarningTypeId" ON "EmployeeBonuses" ("EarningTypeId");""");
}

static async Task EnsurePayrollLineTaxableIncomeColumnAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    var columns = await db.Database.SqlQueryRaw<string>("SELECT name FROM pragma_table_info('PayrollLines')").ToListAsync();
    if (!columns.Contains("TaxableIncome"))
        await db.Database.ExecuteSqlRawAsync("""ALTER TABLE "PayrollLines" ADD COLUMN "TaxableIncome" TEXT NOT NULL DEFAULT 0;""");
}

static async Task EnsureSupportTicketsTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "SupportTickets" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_SupportTickets" PRIMARY KEY,
            "TicketNumber" TEXT NOT NULL,
            "CompanyId" TEXT NOT NULL,
            "CreatedByUserId" TEXT NOT NULL,
            "Subject" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "Type" INTEGER NOT NULL,
            "Status" INTEGER NOT NULL,
            "ClosedAt" TEXT NULL,
            "ClosedByUserId" TEXT NULL,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            CONSTRAINT "FK_SupportTickets_Companies_CompanyId"
                FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_SupportTickets_TicketNumber"
            ON "SupportTickets" ("TicketNumber");
        CREATE INDEX IF NOT EXISTS "IX_SupportTickets_CompanyId_CreatedAt"
            ON "SupportTickets" ("CompanyId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_SupportTickets_CreatedByUserId"
            ON "SupportTickets" ("CreatedByUserId");
        """);
}

static async Task EnsureDefaultDepartmentsAsync(AppDbContext db)
{
    var companyIdsWithSystemDept = await db.Departments
        .Where(d => d.IsSystemDepartment && !d.IsDeleted)
        .Select(d => d.CompanyId)
        .Distinct()
        .ToListAsync();

    var companiesNeedingDefault = await db.Companies
        .Where(c => !c.IsDeleted && !companyIdsWithSystemDept.Contains(c.Id))
        .Select(c => c.Id)
        .ToListAsync();

    if (companiesNeedingDefault.Count == 0)
        return;

    foreach (var companyId in companiesNeedingDefault)
    {
        // If a department literally named "General" already exists, promote it instead of creating a duplicate.
        var existingGeneral = await db.Departments.FirstOrDefaultAsync(
            d => d.CompanyId == companyId && !d.IsDeleted && d.Name.ToLower() == "general");
        if (existingGeneral is not null)
        {
            existingGeneral.IsSystemDepartment = true;
        }
        else
        {
            db.Departments.Add(new Payroll.Domain.Employees.Department
            {
                CompanyId = companyId,
                Name = "General",
                IsSystemDepartment = true
            });
        }
    }

    await db.SaveChangesAsync();
}

static async Task EnsureCompanySubscriptionsTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "CompanySubscriptions" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CompanySubscriptions" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "CompanyId" TEXT NOT NULL,
            "PlanCode" TEXT NOT NULL,
            "Status" INTEGER NOT NULL,
            "TrialStartDate" TEXT NULL,
            "TrialEndDate" TEXT NULL,
            "CurrentPeriodStart" TEXT NULL,
            "CurrentPeriodEnd" TEXT NULL,
            "Price" TEXT NOT NULL,
            "Currency" TEXT NOT NULL,
            "PaymentProvider" TEXT NULL,
            "PaymentCustomerReference" TEXT NULL,
            "PaymentSubscriptionReference" TEXT NULL,
            "PaymentMethodReference" TEXT NULL,
            "CancelledAt" TEXT NULL,
            CONSTRAINT "FK_CompanySubscriptions_Companies_CompanyId"
                FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CompanySubscriptions_CompanyId"
            ON "CompanySubscriptions" ("CompanyId");
        """);
}

static async Task EnsureCompanyPaymentProfilesTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "CompanyPaymentProfiles" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_CompanyPaymentProfiles" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "CompanyId" TEXT NOT NULL,
            "AccountHolderName" TEXT NULL,
            "BankName" TEXT NULL,
            "AccountType" TEXT NULL,
            "AccountLast4" TEXT NULL,
            "PaymentProvider" TEXT NULL,
            "PaymentCustomerReference" TEXT NULL,
            "PaymentMethodReference" TEXT NULL,
            "MandateReference" TEXT NULL,
            "CardBrand" TEXT NULL,
            CONSTRAINT "FK_CompanyPaymentProfiles_Companies_CompanyId"
                FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_CompanyPaymentProfiles_CompanyId"
            ON "CompanyPaymentProfiles" ("CompanyId");
        """);
}

static async Task EnsurePaymentWebhookEventsTableAsync(AppDbContext db)
{
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        return;

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "PaymentWebhookEvents" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_PaymentWebhookEvents" PRIMARY KEY,
            "CreatedAt" TEXT NOT NULL,
            "CreatedBy" TEXT NOT NULL,
            "ModifiedAt" TEXT NULL,
            "ModifiedBy" TEXT NULL,
            "IsDeleted" INTEGER NOT NULL,
            "DeletedBy" TEXT NULL,
            "DeletedAt" TEXT NULL,
            "EventId" TEXT NOT NULL,
            "EventType" TEXT NOT NULL,
            "CompanyId" TEXT NULL,
            "ProcessedAt" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentWebhookEvents_EventId"
            ON "PaymentWebhookEvents" ("EventId");
        """);
}

static async Task EnsureDefaultSubscriptionsAsync(AppDbContext db)
{
    var companyIdsWithSubscription = await db.CompanySubscriptions.Select(s => s.CompanyId).ToListAsync();
    var companiesNeedingSubscription = await db.Companies
        .Where(c => !c.IsDeleted && !companyIdsWithSubscription.Contains(c.Id))
        .ToListAsync();

    if (companiesNeedingSubscription.Count == 0) return;

    var now = DateTime.UtcNow;
    foreach (var company in companiesNeedingSubscription)
    {
        db.CompanySubscriptions.Add(new Payroll.Domain.Billing.CompanySubscription
        {
            CompanyId = company.Id,
            PlanCode = Payroll.Domain.Billing.PlanCatalog.FreeTrialCode,
            Status = Payroll.Domain.Billing.SubscriptionStatus.FreeTrial,
            TrialStartDate = now,
            TrialEndDate = now.AddMonths(1),
            Price = 0
        });
    }

    await db.SaveChangesAsync();
}
