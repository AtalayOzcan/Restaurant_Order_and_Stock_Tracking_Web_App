using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddImpersonationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "impersonation_tokens",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SysAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TargetTenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Admin"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_impersonation_tokens", x => x.TokenId);
                });

            migrationBuilder.CreateIndex(
                name: "ix_impersonation_tokens_active",
                table: "impersonation_tokens",
                columns: new[] { "UsedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "ix_impersonation_tokens_sysadmin",
                table: "impersonation_tokens",
                column: "SysAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "impersonation_tokens");
        }
    }
}
