using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC_Parts_Scrapper.Migrations
{
    /// <inheritdoc />
    public partial class temp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "temp",
                table: "ScrapedItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "temp",
                table: "ScrapedItems");
        }
    }
}
