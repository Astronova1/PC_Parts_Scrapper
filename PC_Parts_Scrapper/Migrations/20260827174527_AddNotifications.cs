using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PC_Parts_Scrapper.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_ApplicationUserId",
                table: "PriceAlerts");

            migrationBuilder.DropIndex(
                name: "IX_PriceAlerts_ApplicationUserId",
                table: "PriceAlerts");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "PriceAlerts");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveAt",
                table: "PriceAlerts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PriceAlertId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_PriceAlerts_PriceAlertId",
                        column: x => x.PriceAlertId,
                        principalTable: "PriceAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PriceAlertId",
                table: "Notifications",
                column: "PriceAlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_UserId",
                table: "PriceAlerts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_UserId",
                table: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts");

            migrationBuilder.DropColumn(
                name: "ActiveAt",
                table: "PriceAlerts");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "PriceAlerts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_ApplicationUserId",
                table: "PriceAlerts",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PriceAlerts_AspNetUsers_ApplicationUserId",
                table: "PriceAlerts",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
