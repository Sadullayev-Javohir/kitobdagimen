using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMorningQuoteVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MorningQuoteVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    IsPositive = table.Column<bool>(type: "boolean", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MorningQuoteVotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MorningQuoteVotes_ChatId_MessageId_IsPositive",
                table: "MorningQuoteVotes",
                columns: new[] { "ChatId", "MessageId", "IsPositive" });

            migrationBuilder.CreateIndex(
                name: "IX_MorningQuoteVotes_ChatId_MessageId_TelegramUserId",
                table: "MorningQuoteVotes",
                columns: new[] { "ChatId", "MessageId", "TelegramUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MorningQuoteVotes");
        }
    }
}
