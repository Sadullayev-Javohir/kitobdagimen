using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Quotes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            // Backfill any pre-existing quotes with a unique random slug so the
            // unique index below can be created (no-op on an empty table).
            migrationBuilder.Sql(
                "UPDATE \"Quotes\" SET \"Slug\" = substr(md5(random()::text || \"Id\"::text), 1, 12) WHERE \"Slug\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Slug",
                table: "Quotes",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_Slug",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Quotes");
        }
    }
}
