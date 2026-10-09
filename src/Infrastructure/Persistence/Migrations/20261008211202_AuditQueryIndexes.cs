using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FarmId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "FarmId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt_Id",
                table: "AuditLogs",
                columns: new[] { "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_FarmId_OccurredAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OccurredAt_Id",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_OccurredAt",
                table: "AuditLogs");
        }
    }
}
