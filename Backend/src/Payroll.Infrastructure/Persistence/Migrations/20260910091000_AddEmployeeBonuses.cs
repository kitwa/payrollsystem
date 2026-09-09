using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddEmployeeBonuses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeBonuses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                PayrollPeriodId = table.Column<Guid>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeBonuses", x => x.Id);
                table.ForeignKey("FK_EmployeeBonuses_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_EmployeeBonuses_PayrollPeriods_PayrollPeriodId", x => x.PayrollPeriodId, "PayrollPeriods", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_EmployeeBonuses_EmployeeId", table: "EmployeeBonuses", column: "EmployeeId");
        migrationBuilder.CreateIndex(name: "IX_EmployeeBonuses_PayrollPeriodId", table: "EmployeeBonuses", column: "PayrollPeriodId");
        migrationBuilder.CreateIndex(name: "IX_EmployeeBonuses_CompanyId_EmployeeId_PayrollPeriodId", table: "EmployeeBonuses", columns: new[] { "CompanyId", "EmployeeId", "PayrollPeriodId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "EmployeeBonuses");
}
