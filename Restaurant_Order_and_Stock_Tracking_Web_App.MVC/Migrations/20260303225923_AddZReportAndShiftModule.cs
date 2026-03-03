using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddZReportAndShiftModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shift_logs_AspNetUsers_ClosedByUserId",
                table: "shift_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_shift_logs_AspNetUsers_OpenedByUserId",
                table: "shift_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_shift_logs",
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

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantType",
                table: "tenants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalWaste",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSales",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalOther",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDiscount",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCash",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCard",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OpenedAt",
                table: "ShiftLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

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
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingBalance",
                table: "ShiftLogs",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldDefaultValue: 0m);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<int>(
                name: "RestaurantType",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalWaste",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalSales",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalOther",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDiscount",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCash",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCard",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpeningBalance",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OpenedAt",
                table: "shift_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

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
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "ClosingBalance",
                table: "shift_logs",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_shift_logs",
                table: "shift_logs",
                column: "ShiftLogId");

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
        }
    }
}
