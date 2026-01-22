using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoLoop.Migrations
{
    /// <inheritdoc />
    public partial class StoreReviews_NoAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_News_NewsId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Stores_StoreId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Comment_CommentId",
                table: "CommentLikes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comment",
                table: "Comment");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Comment");

            migrationBuilder.RenameTable(
                name: "Comment",
                newName: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_StoreId",
                table: "Comments",
                newName: "IX_Comments_StoreId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_NewsId",
                table: "Comments",
                newName: "IX_Comments_NewsId");

            migrationBuilder.AddColumn<string>(
                name: "EditToken",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitorKey",
                table: "Comments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitorName",
                table: "Comments",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "CommentHelpfuls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    VisitorKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentHelpfuls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentHelpfuls_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentHelpfuls_CommentId_VisitorKey",
                table: "CommentHelpfuls",
                columns: new[] { "CommentId", "VisitorKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Comments_CommentId",
                table: "CommentLikes",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_News_NewsId",
                table: "Comments",
                column: "NewsId",
                principalTable: "News",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Stores_StoreId",
                table: "Comments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_Comments_CommentId",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_News_NewsId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Stores_StoreId",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "CommentHelpfuls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "EditToken",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "VisitorKey",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "VisitorName",
                table: "Comments");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Comment");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_StoreId",
                table: "Comment",
                newName: "IX_Comment_StoreId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_NewsId",
                table: "Comment",
                newName: "IX_Comment_NewsId");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Comment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comment",
                table: "Comment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_News_NewsId",
                table: "Comment",
                column: "NewsId",
                principalTable: "News",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Stores_StoreId",
                table: "Comment",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_Comment_CommentId",
                table: "CommentLikes",
                column: "CommentId",
                principalTable: "Comment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
