using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryExpiresAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Stories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing stories predate the duration feature — give them a 24h window from creation
            // so they don't all instantly count as expired (no-op on an empty Stories table).
            migrationBuilder.Sql("UPDATE \"Stories\" SET \"ExpiresAt\" = \"CreatedAt\" + interval '24 hours';");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_ExpiresAt",
                table: "Stories",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stories_ExpiresAt",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Stories");
        }
    }
}
