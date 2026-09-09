using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddCompanyLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(name: "LogoData", table: "Companies", type: "BLOB", nullable: true);
        migrationBuilder.AddColumn<string>(name: "LogoContentType", table: "Companies", type: "TEXT", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LogoData", table: "Companies");
        migrationBuilder.DropColumn(name: "LogoContentType", table: "Companies");
    }
}
