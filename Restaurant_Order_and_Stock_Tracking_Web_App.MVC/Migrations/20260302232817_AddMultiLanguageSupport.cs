using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiLanguageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "menu_items",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionRu",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "menu_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "categories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "DescriptionRu",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "menu_items");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "categories");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "menu_items",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
