using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_guest_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_guest_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_guest_sessions_chat_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "chat_contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_guest_sessions_ContactId_IsRevoked_ExpiresAt",
                table: "chat_guest_sessions",
                columns: new[] { "ContactId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_guest_sessions_TokenHash",
                table: "chat_guest_sessions",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_guest_sessions");
        }
    }
}
