using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeDeductionPayrollPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayrollPeriodId",
                table: "EmployeeDeductions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDeductions_PayrollPeriodId",
                table: "EmployeeDeductions",
                column: "PayrollPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDeductions_PayrollPeriods_PayrollPeriodId",
                table: "EmployeeDeductions",
                column: "PayrollPeriodId",
                principalTable: "PayrollPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDeductions_PayrollPeriods_PayrollPeriodId",
                table: "EmployeeDeductions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDeductions_PayrollPeriodId",
                table: "EmployeeDeductions");

            migrationBuilder.DropColumn(
                name: "PayrollPeriodId",
                table: "EmployeeDeductions");
        }
    }
}
