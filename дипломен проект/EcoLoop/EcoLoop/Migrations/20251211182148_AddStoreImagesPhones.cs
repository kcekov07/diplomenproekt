using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLoop.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreImagesPhones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Stores",
                newName: "WorkingHours");

            migrationBuilder.CreateTable(
                name: "StoreImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreImages_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StorePhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorePhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorePhones_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreImages_StoreId",
                table: "StoreImages",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePhones_StoreId",
                table: "StorePhones",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreImages");

            migrationBuilder.DropTable(
                name: "StorePhones");

            migrationBuilder.RenameColumn(
                name: "WorkingHours",
                table: "Stores",
                newName: "Phone");
        }
    }
}
