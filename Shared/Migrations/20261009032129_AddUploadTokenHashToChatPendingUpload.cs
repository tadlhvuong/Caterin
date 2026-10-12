using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadTokenHashToChatPendingUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadTokenHash",
                table: "chat_pending_upload",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_pending_upload_UploadTokenHash_Status_ExpiresAt",
                table: "chat_pending_upload",
                columns: new[] { "UploadTokenHash", "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_chat_pending_upload_UploadTokenHash_Status_ExpiresAt",
                table: "chat_pending_upload");

            migrationBuilder.DropColumn(
                name: "UploadTokenHash",
                table: "chat_pending_upload");
        }
    }
}
