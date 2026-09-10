using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddSupportTickets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SupportTickets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TicketNumber = table.Column<string>(type: "TEXT", nullable: false),
                CompanyId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                Subject = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ClosedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SupportTickets", x => x.Id);
                table.ForeignKey("FK_SupportTickets_Companies_CompanyId", x => x.CompanyId, "Companies", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_SupportTickets_TicketNumber", "SupportTickets", "TicketNumber", unique: true);
        migrationBuilder.CreateIndex("IX_SupportTickets_CompanyId_CreatedAt", "SupportTickets", new[] { "CompanyId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_SupportTickets_CreatedByUserId", "SupportTickets", "CreatedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("SupportTickets");
}