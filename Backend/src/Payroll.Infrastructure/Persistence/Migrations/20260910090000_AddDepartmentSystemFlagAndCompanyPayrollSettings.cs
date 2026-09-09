using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddDepartmentSystemFlagAndCompanyPayrollSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSystemDepartment",
            table: "Departments",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsUifEnabled",
            table: "Companies",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsSdlEnabled",
            table: "Companies",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsSystemDepartment", table: "Departments");
        migrationBuilder.DropColumn(name: "IsUifEnabled", table: "Companies");
        migrationBuilder.DropColumn(name: "IsSdlEnabled", table: "Companies");
    }
}
