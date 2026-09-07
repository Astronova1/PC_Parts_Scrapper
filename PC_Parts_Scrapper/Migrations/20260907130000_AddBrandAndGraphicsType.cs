using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC_Parts_Scrapper.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandAndGraphicsType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "ScrapedItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GraphicsType",
                table: "Products",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "ScrapedItems");

            migrationBuilder.DropColumn(
                name: "GraphicsType",
                table: "Products");
        }
    }
}
