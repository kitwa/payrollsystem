using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Payroll.Infrastructure;
using Payroll.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // Serilog
    // ============================================================

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration));


    // ============================================================
    // Infrastructure
    // ============================================================

    builder.Services.AddInfrastructure(builder.Configuration);


    // ============================================================
    // MediatR
    // ============================================================

    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(
            typeof(Payroll.Application.Auth.Commands.LoginCommand).Assembly);

        cfg.AddOpenBehavior(
            typeof(Payroll.Application.Common.Behaviours.ValidationBehaviour<,>));

        cfg.AddOpenBehavior(
            typeof(Payroll.Application.Common.Behaviours.LoggingBehaviour<,>));
    });


    // ============================================================
    // Controllers
    // ============================================================

    builder.Services.AddControllers();

    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
        options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });


    // ============================================================
    // CORS
    // ============================================================

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200")
                .AllowCredentials();
        });
    });


    // ============================================================
    // Swagger
    // ============================================================

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Payroll SA API",
                Version = "v1"
            });

        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description = "JWT. Example: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
    });


    // ============================================================
    // HTTP Context
    // ============================================================

    builder.Services.AddHttpContextAccessor();


    // ============================================================
    // Build application
    // ============================================================

    var app = builder.Build();


    // ============================================================
    // Database migration + seed
    // ============================================================

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var db = services.GetRequiredService<AppDbContext>();

        var userManager =
            services.GetRequiredService<
                Microsoft.AspNetCore.Identity.UserManager<
                    Payroll.Domain.Identity.AppUser>>();

        var roleManager =
            services.GetRequiredService<
                Microsoft.AspNetCore.Identity.RoleManager<
                    Payroll.Domain.Identity.AppRole>>();

        try
        {
            // Apply all EF Core migrations.
            //
            // This now runs against MariaDB/MySQL through Pomelo.
            await db.Database.MigrateAsync();

            // Create default "General" departments where required.
            await EnsureDefaultDepartmentsAsync(db);

            // Create default payroll items for existing companies.
            await EnsureDefaultPayrollItemsAsync(db);

            // Create default Free Trial subscriptions
            // for companies that don't have one.
            await EnsureDefaultSubscriptionsAsync(db);

            // Seed users, roles and other initial data.
            await Seed.SeedAsync(
                db,
                userManager,
                roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogError(
                ex,
                "An error occurred during database migration or seeding.");

            throw;
        }
    }


    // ============================================================
    // Hangfire recurring jobs
    // ============================================================

    using (var scope = app.Services.CreateScope())
    {
        var recurringJobs =
            scope.ServiceProvider
                .GetRequiredService<IRecurringJobManager>();

        // First month free, all features included,
        // capped at 5 employees.
        //
        // This job checks daily for expired trials.
        recurringJobs.AddOrUpdate<
            Payroll.Application.Common.Interfaces.ISubscriptionService>(
                "expire-overdue-trials",
                svc => svc.ExpireOverdueTrialsAsync(
                    CancellationToken.None),
                Cron.Daily);
    }


    // ============================================================
    // Middleware
    // ============================================================

    app.UseSerilogRequestLogging();

    app.UseMiddleware<
        Payroll.Api.Middleware.ExceptionMiddleware>();

    app.UseMiddleware<
        Payroll.Api.Middleware.AuditMiddleware>();

    app.UseHttpsRedirection();

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; img-src 'self' data: blob:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; font-src 'self' https://cdn.jsdelivr.net; connect-src 'self' http://localhost:4200 https://localhost:4200 http://localhost:5099 https://localhost:5099";
        await next();
    });

    app.UseRouting();

    app.UseRateLimiter();

    app.UseCors("CorsPolicy");

    app.UseAuthentication();

    app.UseAuthorization();

    app.UseMiddleware<
        Payroll.Api.Middleware.CompanyStatusMiddleware>();


    // ============================================================
    // Swagger
    // ============================================================

    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Payroll SA v1");
    });


    // ============================================================
    // Static Angular files
    // ============================================================

    app.UseDefaultFiles();

    app.UseStaticFiles();


    // ============================================================
    // Hangfire Dashboard
    // ============================================================

    app.UseHangfireDashboard(
        "/hangfire",
        new DashboardOptions
        {
            Authorization =
            [
                new Payroll.Api.Middleware.HangfireAuthFilter()
            ]
        });


    // ============================================================
    // Controllers
    // ============================================================

    app.MapControllers();


    // ============================================================
    // Angular SPA fallback
    // ============================================================

    app.MapFallbackToFile("index.html");


    // ============================================================
    // Run
    // ============================================================

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(
        ex,
        "Application startup failed.");

    throw;
}
finally
{
    Log.CloseAndFlush();
}


// ================================================================
// Default Departments
// ================================================================

static async Task EnsureDefaultDepartmentsAsync(
    AppDbContext db)
{
    var companyIdsWithSystemDept =
        await db.Departments
            .Where(d =>
                d.IsSystemDepartment &&
                !d.IsDeleted)
            .Select(d => d.CompanyId)
            .Distinct()
            .ToListAsync();

    var companiesNeedingDefault =
        await db.Companies
            .Where(c =>
                !c.IsDeleted &&
                !companyIdsWithSystemDept.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync();

    if (companiesNeedingDefault.Count == 0)
        return;

    foreach (var companyId in companiesNeedingDefault)
    {
        // If "General" already exists,
        // promote it to the system department.
        var existingGeneral =
            await db.Departments.FirstOrDefaultAsync(
                d =>
                    d.CompanyId == companyId &&
                    !d.IsDeleted &&
                    d.Name.ToLower() == "general");

        if (existingGeneral is not null)
        {
            existingGeneral.IsSystemDepartment = true;
        }
        else
        {
            db.Departments.Add(
                new Payroll.Domain.Employees.Department
                {
                    CompanyId = companyId,
                    Name = "General",
                    IsSystemDepartment = true
                });
        }
    }

    await db.SaveChangesAsync();
}


// ================================================================
// Default Payroll Items
// ================================================================

static async Task EnsureDefaultPayrollItemsAsync(AppDbContext db)
{
    var companies = await db.Companies
        .Where(c => !c.IsDeleted)
        .Select(c => c.Id)
        .ToListAsync();
    var defaultEarnings = new[]
    {
        (Name: "Bonus", Code: "BONUS", IsTaxable: true),
        (Name: "Overtime", Code: "OVERTIME", IsTaxable: true),
        (Name: "Commission", Code: "COMMISSION", IsTaxable: true)
    };
    var defaultDeductions = new[]
    {
        (Name: "Staff Loan", Code: "LOAN", IsEmployerContribution: false),
        (Name: "Medical Aid", Code: "MEDICAL_AID", IsEmployerContribution: true),
        (Name: "Pension", Code: "PENSION", IsEmployerContribution: true),
        (Name: "Salary Advance", Code: "ADVANCE", IsEmployerContribution: false)
    };

    foreach (var companyId in companies)
    {
        var earningCodes = await db.EarningTypes
            .Where(t => t.CompanyId == companyId && !t.IsDeleted)
            .Select(t => t.Code)
            .ToListAsync();
        foreach (var item in defaultEarnings.Where(item => !earningCodes.Contains(item.Code)))
            db.EarningTypes.Add(new Payroll.Domain.Settings.EarningType
            {
                CompanyId = companyId, Name = item.Name, Code = item.Code, IsTaxable = item.IsTaxable
            });

        var deductionCodes = await db.DeductionTypes
            .Where(t => t.CompanyId == companyId && !t.IsDeleted)
            .Select(t => t.Code)
            .ToListAsync();
        foreach (var item in defaultDeductions.Where(item => !deductionCodes.Contains(item.Code)))
            db.DeductionTypes.Add(new Payroll.Domain.Settings.DeductionType
            {
                CompanyId = companyId, Name = item.Name, Code = item.Code,
                IsEmployerContribution = item.IsEmployerContribution
            });
    }

    await db.SaveChangesAsync();
}


// ================================================================
// Default Subscriptions
// ================================================================

static async Task EnsureDefaultSubscriptionsAsync(
    AppDbContext db)
{
    var companyIdsWithSubscription =
        await db.CompanySubscriptions
            .Select(s => s.CompanyId)
            .ToListAsync();

    var companiesNeedingSubscription =
        await db.Companies
            .Where(c =>
                !c.IsDeleted &&
                !companyIdsWithSubscription.Contains(c.Id))
            .ToListAsync();

    if (companiesNeedingSubscription.Count == 0)
        return;

    var now = DateTime.UtcNow;

    foreach (var company in companiesNeedingSubscription)
    {
        db.CompanySubscriptions.Add(
            new Payroll.Domain.Billing.CompanySubscription
            {
                CompanyId = company.Id,

                PlanCode =
                    Payroll.Domain.Billing.PlanCatalog.FreeTrialCode,

                Status =
                    Payroll.Domain.Billing.SubscriptionStatus.FreeTrial,

                TrialStartDate = now,

                TrialEndDate =
                    now.AddMonths(1),

                Price = 0
            });
    }

    await db.SaveChangesAsync();
}