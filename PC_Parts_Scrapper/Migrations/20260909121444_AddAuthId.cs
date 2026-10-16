using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC_Parts_Scrapper.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "temp",
                table: "ScrapedItems");

            migrationBuilder.DropColumn(
                name: "tempe",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "Auth0Id",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Auth0Id",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "temp",
                table: "ScrapedItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tempe",
                table: "Products",
                type: "text",
                nullable: true);
        }
    }
}
