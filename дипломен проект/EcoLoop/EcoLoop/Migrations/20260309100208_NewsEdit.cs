using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLoop.Migrations
{
    /// <inheritdoc />
    public partial class NewsEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsLikes_NewsId_VisitorKey",
                table: "NewsLikes");

            migrationBuilder.DropColumn(
                name: "VisitorKey",
                table: "NewsLikes");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "NewsLikes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLikes_NewsId_UserId",
                table: "NewsLikes",
                columns: new[] { "NewsId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsLikes_NewsId_UserId",
                table: "NewsLikes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NewsLikes");

            migrationBuilder.AddColumn<string>(
                name: "VisitorKey",
                table: "NewsLikes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLikes_NewsId_VisitorKey",
                table: "NewsLikes",
                columns: new[] { "NewsId", "VisitorKey" },
                unique: true);
        }
    }
}
