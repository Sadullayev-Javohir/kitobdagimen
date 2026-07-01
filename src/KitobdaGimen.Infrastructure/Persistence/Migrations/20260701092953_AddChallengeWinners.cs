using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeWinners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeWinners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    PagesRead = table.Column<int>(type: "integer", nullable: false),
                    BooksRead = table.Column<int>(type: "integer", nullable: false),
                    ActiveDays = table.Column<int>(type: "integer", nullable: false),
                    AvgPagesPerDay = table.Column<double>(type: "double precision", nullable: false),
                    GiftBookTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    GiftBookAuthor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GiftBookCoverUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    GiftedByUserId = table.Column<int>(type: "integer", nullable: true),
                    GiftedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AnnouncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeWinners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeWinners_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeWinnerLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChallengeWinnerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeWinnerLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeWinnerLikes_ChallengeWinners_ChallengeWinnerId",
                        column: x => x.ChallengeWinnerId,
                        principalTable: "ChallengeWinners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeWinnerLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWinnerLikes_ChallengeWinnerId_UserId",
                table: "ChallengeWinnerLikes",
                columns: new[] { "ChallengeWinnerId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWinnerLikes_UserId",
                table: "ChallengeWinnerLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWinners_UserId",
                table: "ChallengeWinners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWinners_Year_Month_Rank",
                table: "ChallengeWinners",
                columns: new[] { "Year", "Month", "Rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeWinners_Year_Month_UserId",
                table: "ChallengeWinners",
                columns: new[] { "Year", "Month", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeWinnerLikes");

            migrationBuilder.DropTable(
                name: "ChallengeWinners");
        }
    }
}
