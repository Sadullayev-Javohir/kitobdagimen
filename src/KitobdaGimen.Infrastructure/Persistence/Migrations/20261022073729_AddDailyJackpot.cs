using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyJackpot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyJackpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: true),
                    QuoteId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramMessageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyJackpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyJackpots_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DailyJackpots_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DailyJackpots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyJackpotComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyJackpotId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyJackpotComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyJackpotComments_DailyJackpots_DailyJackpotId",
                        column: x => x.DailyJackpotId,
                        principalTable: "DailyJackpots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyJackpotComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyJackpotLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DailyJackpotId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyJackpotLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyJackpotLikes_DailyJackpots_DailyJackpotId",
                        column: x => x.DailyJackpotId,
                        principalTable: "DailyJackpots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyJackpotLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpotComments_DailyJackpotId",
                table: "DailyJackpotComments",
                column: "DailyJackpotId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpotComments_UserId",
                table: "DailyJackpotComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpotLikes_DailyJackpotId_UserId",
                table: "DailyJackpotLikes",
                columns: new[] { "DailyJackpotId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpotLikes_UserId",
                table: "DailyJackpotLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpots_Date",
                table: "DailyJackpots",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpots_PostId",
                table: "DailyJackpots",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpots_QuoteId",
                table: "DailyJackpots",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyJackpots_UserId",
                table: "DailyJackpots",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyJackpotComments");

            migrationBuilder.DropTable(
                name: "DailyJackpotLikes");

            migrationBuilder.DropTable(
                name: "DailyJackpots");
        }
    }
}
