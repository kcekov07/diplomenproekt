using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLoop.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreEcoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EcoTags",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Stores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Stores",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasDelivery",
                table: "Stores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasRefillStation",
                table: "Stores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Stores",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "EcoTags",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "HasDelivery",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "HasRefillStation",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Stores");
        }
    }
}
