using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLogs",
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
                CompanyId = table.Column<Guid>(type: "TEXT", nullable: true),
                UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                UserEmail = table.Column<string>(type: "TEXT", nullable: true),
                IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                HttpMethod = table.Column<string>(type: "TEXT", nullable: false),
                Path = table.Column<string>(type: "TEXT", nullable: false),
                StatusCode = table.Column<int>(type: "INTEGER", nullable: false),
                Action = table.Column<string>(type: "TEXT", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CompanyId_OccurredAt",
            table: "AuditLogs",
            columns: new[] { "CompanyId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_OccurredAt",
            table: "AuditLogs",
            column: "OccurredAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "AuditLogs");
}
