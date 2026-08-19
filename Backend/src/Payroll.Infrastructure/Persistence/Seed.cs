using Microsoft.AspNetCore.Identity;
using Payroll.Domain.Identity;
using Payroll.Domain.Tax;
using Payroll.Domain.Settings;
using Payroll.Shared;

namespace Payroll.Infrastructure.Persistence;

public static class Seed
{
    public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
        await SeedTaxYearAsync(db);
        await SeedLeaveTypesAsync(db);
    }

    private static async Task SeedRolesAsync(RoleManager<AppRole> roleManager)
    {
        string[] roles = [Constants.Roles.SuperAdmin, Constants.Roles.Admin, Constants.Roles.PayrollManager, Constants.Roles.Employee];
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole(role));
    }

    private static async Task SeedAdminUserAsync(UserManager<AppUser> userManager)
    {
        if (await userManager.FindByEmailAsync("admin@payrollsa.co.za") is not null) return;

        var admin = new AppUser
        {
            UserName = "admin@payrollsa.co.za",
            Email = "admin@payrollsa.co.za",
            FirstName = "System",
            LastName = "Admin",
            EmailConfirmed = true,
            IsActive = true
        };

        await userManager.CreateAsync(admin, "Admin@1234!");
        await userManager.AddToRolesAsync(admin, [Constants.Roles.SuperAdmin, Constants.Roles.Admin]);
    }

    private static async Task SeedTaxYearAsync(AppDbContext db)
    {
        if (db.TaxYears.Any()) return;

        // 2025/2026 tax year — rates from SARS
        var taxYear = new TaxYear
        {
            Year = 2025,
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2026, 2, 28),
            UifMonthlyEarningsCeiling = 17712,
            UifContributionRate = 1,
            SdlRate = 1,
            IsActive = true,
            TaxTables =
            [
                new TaxTable { IncomeFrom = 1, IncomeTo = 237100, BaseTax = 0, MarginalRate = 18 },
                new TaxTable { IncomeFrom = 237101, IncomeTo = 370500, BaseTax = 42678, MarginalRate = 26 },
                new TaxTable { IncomeFrom = 370501, IncomeTo = 512800, BaseTax = 77362, MarginalRate = 31 },
                new TaxTable { IncomeFrom = 512801, IncomeTo = 673000, BaseTax = 121475, MarginalRate = 36 },
                new TaxTable { IncomeFrom = 673001, IncomeTo = 857900, BaseTax = 179147, MarginalRate = 39 },
                new TaxTable { IncomeFrom = 857901, IncomeTo = 1817000, BaseTax = 251258, MarginalRate = 41 },
                new TaxTable { IncomeFrom = 1817001, IncomeTo = decimal.MaxValue, BaseTax = 644489, MarginalRate = 45 }
            ],
            TaxThresholds =
            [
                new TaxThreshold { AgeGroup = "Under65", ThresholdAmount = 95750 },
                new TaxThreshold { AgeGroup = "65to74", ThresholdAmount = 148217 },
                new TaxThreshold { AgeGroup = "75AndOver", ThresholdAmount = 165689 }
            ],
            TaxRebates =
            [
                new TaxRebate { RebateType = "Primary", Amount = 17235 },
                new TaxRebate { RebateType = "Secondary", Amount = 9444 },
                new TaxRebate { RebateType = "Tertiary", Amount = 3145 }
            ]
        };

        db.TaxYears.Add(taxYear);
        await db.SaveChangesAsync();
    }

    private static async Task SeedLeaveTypesAsync(AppDbContext db)
    {
        if (db.LeaveTypes.Any()) return;

        // Seeded without a company — global templates; companies can create their own
        var types = new[]
        {
            new LeaveType { CompanyId = Guid.Empty, Name = "Annual Leave", DefaultEntitlementDays = 15, IsPaid = true, RequiresApproval = true },
            new LeaveType { CompanyId = Guid.Empty, Name = "Sick Leave", DefaultEntitlementDays = 30, IsPaid = true, RequiresApproval = false },
            new LeaveType { CompanyId = Guid.Empty, Name = "Family Responsibility", DefaultEntitlementDays = 3, IsPaid = true, RequiresApproval = true },
            new LeaveType { CompanyId = Guid.Empty, Name = "Maternity Leave", DefaultEntitlementDays = 120, IsPaid = false, RequiresApproval = true },
            new LeaveType { CompanyId = Guid.Empty, Name = "Paternity Leave", DefaultEntitlementDays = 10, IsPaid = false, RequiresApproval = true },
            new LeaveType { CompanyId = Guid.Empty, Name = "Unpaid Leave", DefaultEntitlementDays = 0, IsPaid = false, RequiresApproval = true }
        };

        db.LeaveTypes.AddRange(types);
        await db.SaveChangesAsync();
    }
}
