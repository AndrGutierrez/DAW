using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrowthGoalsAndPaddockPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MapHeight",
                table: "Paddocks",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MapWidth",
                table: "Paddocks",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MapX",
                table: "Paddocks",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MapY",
                table: "Paddocks",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxStayDays",
                table: "Paddocks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetDailyGainKg",
                table: "Animals",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ThresholdValue",
                table: "AlertRules",
                type: "numeric(12,4)",
                precision: 12,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapHeight",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "MapWidth",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "MaxStayDays",
                table: "Paddocks");

            migrationBuilder.DropColumn(
                name: "TargetDailyGainKg",
                table: "Animals");

            migrationBuilder.AlterColumn<decimal>(
                name: "ThresholdValue",
                table: "AlertRules",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)",
                oldPrecision: 12,
                oldScale: 4,
                oldNullable: true);
        }
    }
}
