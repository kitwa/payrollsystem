using System.Text;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Payroll.Application.Common.Interfaces;
using Payroll.Domain.Identity;
using Payroll.Infrastructure.PayrollEngine;
using Payroll.Infrastructure.PayrollEngine.Calculators;
using Payroll.Infrastructure.PayrollEngine.Interfaces;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Services;
using Payroll.Shared;
using QuestPDF.Infrastructure;

namespace Payroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ============================================================
        // Entity Framework Core / MariaDB
        // ============================================================

        var connectionString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            options
                .UseMySql(
                    connectionString,
                    ServerVersion.AutoDetect(connectionString),
                    mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IAppDbContext>(
            sp => sp.GetRequiredService<AppDbContext>());

        // ============================================================
        // ASP.NET Core Identity
        // ============================================================

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ============================================================
        // JWT Authentication
        // ============================================================

        var jwtKey = JwtKeyConfiguration.GetKeyBytes(config);

        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(jwtKey),

                        ValidateIssuer = false,
                        ValidateAudience = false,

                        ClockSkew = TimeSpan.Zero
                    };
            });

        // ============================================================
        // Authorization Policies
        // ============================================================

        services.AddAuthorizationBuilder()
            .AddPolicy(
                Constants.Policies.RequireAdminRole,
                policy => policy.RequireRole(
                    Constants.Roles.Admin,
                    Constants.Roles.SuperAdmin))

            .AddPolicy(
                Constants.Policies.RequirePayrollManagerRole,
                policy => policy.RequireRole(
                    Constants.Roles.PayrollManager,
                    Constants.Roles.Admin,
                    Constants.Roles.SuperAdmin))

            .AddPolicy(
                Constants.Policies.RequireEmployeeRole,
                policy => policy.RequireRole(
                    Constants.Roles.Employee,
                    Constants.Roles.PayrollManager,
                    Constants.Roles.Admin,
                    Constants.Roles.SuperAdmin));

        // ============================================================
        // Application Services
        // ============================================================

        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<
            IPayrollEngine,
            PayrollEngine.PayrollEngine>();

        services.AddScoped<IPdfService, PdfService>();

        services.AddScoped<IEmailService, EmailService>();

        services.AddSingleton<ISupportNotificationSettings,
            SupportNotificationSettings>();

        services.AddScoped<ISubscriptionService,
            Payroll.Application.Billing.SubscriptionService>();

        services.AddScoped<IPaymentGateway,
            MockPaymentGateway>();

        // ============================================================
        // Payroll Calculators
        // Registered in execution order
        // ============================================================

        services.AddScoped<IPayrollCalculator,
            BasicSalaryCalculator>();

        services.AddScoped<IPayrollCalculator,
            UnpaidLeaveCalculator>();

        services.AddScoped<IPayrollCalculator,
            PAYECalculator>();

        services.AddScoped<IPayrollCalculator,
            UIFCalculator>();

        services.AddScoped<IPayrollCalculator,
            SDLCalculator>();

        // ============================================================
        // Hangfire
        // ============================================================

        // Hangfire is currently using in-memory storage.
        // This is fine for development but jobs will be lost when
        // the application restarts.
        services.AddHangfire(configuration =>
            configuration.UseInMemoryStorage());

        services.AddHangfireServer();

        // ============================================================
        // QuestPDF
        // ============================================================

        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}