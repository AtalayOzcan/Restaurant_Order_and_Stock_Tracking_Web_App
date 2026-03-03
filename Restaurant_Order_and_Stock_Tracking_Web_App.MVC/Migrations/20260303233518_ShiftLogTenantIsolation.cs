using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class ShiftLogTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftLogs_AspNetUsers_ClosedByUserId",
                table: "ShiftLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftLogs_AspNetUsers_OpenedByUserId",
                table: "ShiftLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShiftLogs",
                table: "ShiftLogs");

            migrationBuilder.RenameTable(
                name: "ShiftLogs",
                newName: "shift_logs");

            migrationBuilder.RenameIndex(
                name: "IX_ShiftLogs_OpenedByUserId",
                table: "shift_logs",
                newName: "IX_shift_logs_OpenedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ShiftLogs_ClosedByUserId",
                table: "shift_logs",
                newName: "IX_shift_logs_ClosedByUserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalWaste",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSales",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalOther",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDiscount",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCash",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCard",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "OpenedByUserId",
                table: "shift_logs",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "shift_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "shift_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<decimal>(
                name: "DifferenceThreshold",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 100m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Difference",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingBalance",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "ClosedByUserId",
                table: "shift_logs",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "shift_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_shift_logs",
                table: "shift_logs",
                column: "ShiftLogId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_logs_TenantId",
                table: "shift_logs",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_shift_logs_AspNetUsers_ClosedByUserId",
                table: "shift_logs",
                column: "ClosedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shift_logs_AspNetUsers_OpenedByUserId",
                table: "shift_logs",
                column: "OpenedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_shift_logs_tenants_TenantId",
                table: "shift_logs",
                column: "TenantId",
                principalTable: "tenants",
                principalColumn: "TenantId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shift_logs_AspNetUsers_ClosedByUserId",
                table: "shift_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_shift_logs_AspNetUsers_OpenedByUserId",
                table: "shift_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_shift_logs_tenants_TenantId",
                table: "shift_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_shift_logs",
                table: "shift_logs");

            migrationBuilder.DropIndex(
                name: "IX_shift_logs_TenantId",
                table: "shift_logs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "shift_logs");

            migrationBuilder.RenameTable(
                name: "shift_logs",
                newName: "ShiftLogs");

            migrationBuilder.RenameIndex(
                name: "IX_shift_logs_OpenedByUserId",
                table: "ShiftLogs",
                newName: "IX_ShiftLogs_OpenedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_shift_logs_ClosedByUserId",
                table: "ShiftLogs",
                newName: "IX_ShiftLogs_ClosedByUserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalWaste",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSales",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalOther",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDiscount",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCash",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCard",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "OpenedByUserId",
                table: "ShiftLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<bool>(
                name: "IsLocked",
                table: "ShiftLogs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "ShiftLogs",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "DifferenceThreshold",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 100m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Difference",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingBalance",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "ClosedByUserId",
                table: "ShiftLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShiftLogs",
                table: "ShiftLogs",
                column: "ShiftLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftLogs_AspNetUsers_ClosedByUserId",
                table: "ShiftLogs",
                column: "ClosedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftLogs_AspNetUsers_OpenedByUserId",
                table: "ShiftLogs",
                column: "OpenedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
