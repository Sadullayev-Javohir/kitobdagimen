using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Posts",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            // Backfill any pre-existing posts with a unique random slug so the
            // unique index below can be created (no-op on an empty table).
            migrationBuilder.Sql(
                "UPDATE \"Posts\" SET \"Slug\" = substr(md5(random()::text || \"Id\"::text), 1, 12) WHERE \"Slug\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Slug",
                table: "Posts",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_Slug",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Posts");
        }
    }
}
