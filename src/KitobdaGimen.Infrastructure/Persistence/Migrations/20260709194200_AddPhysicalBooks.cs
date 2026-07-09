using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitobdaGimen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhysicalBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhysicalBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: true),
                    ManualTitle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ManualAuthor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalBooks_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhysicalBooks_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalBookReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhysicalBookId = table.Column<int>(type: "integer", nullable: false),
                    ReserverId = table.Column<int>(type: "integer", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalBookReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhysicalBookReservations_PhysicalBooks_PhysicalBookId",
                        column: x => x.PhysicalBookId,
                        principalTable: "PhysicalBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhysicalBookReservations_Users_ReserverId",
                        column: x => x.ReserverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBookReservations_IsConfirmed_ExpiresAt",
                table: "PhysicalBookReservations",
                columns: new[] { "IsConfirmed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBookReservations_PhysicalBookId",
                table: "PhysicalBookReservations",
                column: "PhysicalBookId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBookReservations_ReserverId",
                table: "PhysicalBookReservations",
                column: "ReserverId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBooks_BookId",
                table: "PhysicalBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBooks_OwnerId_Status",
                table: "PhysicalBooks",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalBooks_Status",
                table: "PhysicalBooks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhysicalBookReservations");

            migrationBuilder.DropTable(
                name: "PhysicalBooks");
        }
    }
}
