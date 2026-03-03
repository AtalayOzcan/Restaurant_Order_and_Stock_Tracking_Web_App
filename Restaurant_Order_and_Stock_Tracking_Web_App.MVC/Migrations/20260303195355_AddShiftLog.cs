using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shift_logs",
                columns: table => new
                {
                    ShiftLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedByUserId = table.Column<string>(type: "text", nullable: false),
                    ClosedByUserId = table.Column<string>(type: "text", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    ClosingBalance = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalSales = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalCash = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalCard = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalOther = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalDiscount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalWaste = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    Difference = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DifferenceThreshold = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 100m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shift_logs", x => x.ShiftLogId);
                    table.ForeignKey(
                        name: "FK_shift_logs_AspNetUsers_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shift_logs_AspNetUsers_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shift_logs_ClosedByUserId",
                table: "shift_logs",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_logs_OpenedByUserId",
                table: "shift_logs",
                column: "OpenedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shift_logs");
        }
    }
}
