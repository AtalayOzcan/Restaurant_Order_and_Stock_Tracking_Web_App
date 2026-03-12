using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddPerf02Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shift_logs_TenantId",
                table: "shift_logs");

            migrationBuilder.DropIndex(
                name: "IX_orders_TenantId",
                table: "orders");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                newName: "ix_order_items_orderid");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                newName: "ix_users_tenantid");

            migrationBuilder.CreateIndex(
                name: "ix_shift_logs_tenantid_isclosed",
                table: "shift_logs",
                columns: new[] { "TenantId", "IsClosed" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenantid_status",
                table: "orders",
                columns: new[] { "TenantId", "OrderStatus" });

            migrationBuilder.CreateIndex(
                name: "ix_users_phonenumber",
                table: "AspNetUsers",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shift_logs_tenantid_isclosed",
                table: "shift_logs");

            migrationBuilder.DropIndex(
                name: "ix_orders_tenantid_status",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_users_phonenumber",
                table: "AspNetUsers");

            migrationBuilder.RenameIndex(
                name: "ix_order_items_orderid",
                table: "order_items",
                newName: "IX_order_items_OrderId");

            migrationBuilder.RenameIndex(
                name: "ix_users_tenantid",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shift_logs_TenantId",
                table: "shift_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId",
                table: "orders",
                column: "TenantId");
        }
    }
}
