using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddEmployeeDeductions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EmployeeDeductions",
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
                Category = table.Column<int>(type: "INTEGER", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                EmployeeAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                EmployerAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeDeductions", x => x.Id);
                table.ForeignKey("FK_EmployeeDeductions_Employees_EmployeeId", x => x.EmployeeId, "Employees", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_EmployeeDeductions_EmployeeId", table: "EmployeeDeductions", column: "EmployeeId");
        migrationBuilder.CreateIndex(name: "IX_EmployeeDeductions_CompanyId_EmployeeId", table: "EmployeeDeductions", columns: new[] { "CompanyId", "EmployeeId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "EmployeeDeductions");
}
