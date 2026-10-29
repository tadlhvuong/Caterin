using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddFlowUploadAvatarAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AvatarMediaFileId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AvatarMediaFileId",
                table: "Users",
                column: "AvatarMediaFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_MediaFiles_AvatarMediaFileId",
                table: "Users",
                column: "AvatarMediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_MediaFiles_AvatarMediaFileId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AvatarMediaFileId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarMediaFileId",
                table: "Users");
        }
    }
}
