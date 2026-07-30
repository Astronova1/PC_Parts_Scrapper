using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC_Parts_Scrapper.Migrations
{
    /// <inheritdoc />
    public partial class Update_ProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ScrapedItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "ScrapedItems");
        }
    }
}
