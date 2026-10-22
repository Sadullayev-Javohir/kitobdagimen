using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramGroupMessageIdsToPostAndQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramChatId",
                table: "Quotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TelegramMessageId",
                table: "Quotes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TelegramChatId",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TelegramMessageId",
                table: "Posts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TelegramMessageId",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "TelegramChatId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "TelegramMessageId",
                table: "Posts");
        }
    }
}
