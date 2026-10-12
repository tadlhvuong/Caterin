using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class TB_ChatPendingUpdoadAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_pending_upload",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MediaFileId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    GuestSessionId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_pending_upload", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_pending_upload_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_pending_upload_chat_guest_sessions_GuestSessionId",
                        column: x => x.GuestSessionId,
                        principalTable: "chat_guest_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chat_pending_upload_chat_messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "chat_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_pending_upload_GuestSessionId",
                table: "chat_pending_upload",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_pending_upload_MediaFileId",
                table: "chat_pending_upload",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_pending_upload_MessageId",
                table: "chat_pending_upload",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_pending_upload_Status_ExpiresAt",
                table: "chat_pending_upload",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_pending_upload");
        }
    }
}
