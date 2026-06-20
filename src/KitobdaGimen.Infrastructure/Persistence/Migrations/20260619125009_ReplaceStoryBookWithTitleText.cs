using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStoryBookWithTitleText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Books_BookId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_BookId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "Stories");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "Stories",
                type: "character varying(140)",
                maxLength: 140,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Stories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Text",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Stories");

            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "Stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_BookId",
                table: "Stories",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Books_BookId",
                table: "Stories",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
