using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class Sprint1_DiscountAndDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_logs_menu_items_MenuItemId",
                table: "stock_logs");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'open'",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "open");

            migrationBuilder.AlterColumn<string>(
                name: "OrderOpenedBy",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "orders",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderItemStatus",
                table: "order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'pending'",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "pending");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "menu_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_logs_menu_items_MenuItemId",
                table: "stock_logs",
                column: "MenuItemId",
                principalTable: "menu_items",
                principalColumn: "MenuItemId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_logs_menu_items_MenuItemId",
                table: "stock_logs");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "menu_items");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "open",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'open'");

            migrationBuilder.AlterColumn<string>(
                name: "OrderOpenedBy",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderItemStatus",
                table: "order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "pending",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "pending");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_logs_menu_items_MenuItemId",
                table: "stock_logs",
                column: "MenuItemId",
                principalTable: "menu_items",
                principalColumn: "MenuItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
