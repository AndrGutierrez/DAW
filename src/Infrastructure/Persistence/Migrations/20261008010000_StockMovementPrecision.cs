using Microsoft.EntityFrameworkCore.Migrations;
namespace Infrastructure.Persistence.Migrations;
public sealed partial class StockMovementPrecision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AlterColumn<decimal>(name: "Quantity", table: "StockMovements", type: "numeric(14,4)", precision: 14, scale: 4, nullable: false, oldClrType: typeof(decimal), oldType: "numeric(12,3)", oldPrecision: 12, oldScale: 3);
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Reducing stock precision could lose recorded quantities. Restore a database backup instead.");
}
