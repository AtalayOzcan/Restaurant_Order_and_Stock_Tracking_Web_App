using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class ReservationInformationAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationGuestCount",
                table: "tables",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationName",
                table: "tables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationPhone",
                table: "tables",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationTime",
                table: "tables",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservationGuestCount",
                table: "tables");

            migrationBuilder.DropColumn(
                name: "ReservationName",
                table: "tables");

            migrationBuilder.DropColumn(
                name: "ReservationPhone",
                table: "tables");

            migrationBuilder.DropColumn(
                name: "ReservationTime",
                table: "tables");
        }
    }
}
