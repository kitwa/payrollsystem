using System.Text;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // EF Core — SQLite by default, SQL Server in production
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite(config.GetConnectionString("DefaultConnection") ?? "Data Source=payroll.db"));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ASP.NET Core Identity
        services.AddIdentityCore<AppUser>(opt =>
        {
            opt.Password.RequireDigit = true;
            opt.Password.RequireUppercase = true;
            opt.Password.RequireLowercase = true;
            opt.Password.RequireNonAlphanumeric = true;
            opt.Password.RequiredLength = 8;
            opt.Lockout.MaxFailedAccessAttempts = 5;
            opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var secret = config["JwtSettings:Secret"] ?? throw new InvalidOperationException("JwtSettings:Secret is required.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Authorization policies
        services.AddAuthorizationBuilder()
            .AddPolicy(Constants.Policies.RequireAdminRole, p => p.RequireRole(Constants.Roles.Admin, Constants.Roles.SuperAdmin))
            .AddPolicy(Constants.Policies.RequirePayrollManagerRole, p => p.RequireRole(Constants.Roles.PayrollManager, Constants.Roles.Admin, Constants.Roles.SuperAdmin))
            .AddPolicy(Constants.Policies.RequireEmployeeRole, p => p.RequireRole(Constants.Roles.Employee, Constants.Roles.PayrollManager, Constants.Roles.Admin, Constants.Roles.SuperAdmin));

        // Application services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPayrollEngine, PayrollEngine.PayrollEngine>();
        services.AddScoped<IPdfService, PdfService>();

        // Payroll calculators — registered in execution order
        services.AddScoped<IPayrollCalculator, BasicSalaryCalculator>();
        services.AddScoped<IPayrollCalculator, PAYECalculator>();
        services.AddScoped<IPayrollCalculator, UIFCalculator>();
        services.AddScoped<IPayrollCalculator, SDLCalculator>();

        // Hangfire
        services.AddHangfire(c => c.UseInMemoryStorage());
        services.AddHangfireServer();

        // QuestPDF community license (free for open source / small business)
        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}
