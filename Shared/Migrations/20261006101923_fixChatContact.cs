using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class fixChatContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestToken",
                table: "chat_contacts");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "chat_guest_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedReason",
                table: "chat_guest_sessions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMerged",
                table: "chat_contacts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MergedAt",
                table: "chat_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MergedIntoContactId",
                table: "chat_contacts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_contacts_MergedIntoContactId",
                table: "chat_contacts",
                column: "MergedIntoContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_contacts_chat_contacts_MergedIntoContactId",
                table: "chat_contacts",
                column: "MergedIntoContactId",
                principalTable: "chat_contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_contacts_chat_contacts_MergedIntoContactId",
                table: "chat_contacts");

            migrationBuilder.DropIndex(
                name: "IX_chat_contacts_MergedIntoContactId",
                table: "chat_contacts");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "chat_guest_sessions");

            migrationBuilder.DropColumn(
                name: "RevokedReason",
                table: "chat_guest_sessions");

            migrationBuilder.DropColumn(
                name: "IsMerged",
                table: "chat_contacts");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "chat_contacts");

            migrationBuilder.DropColumn(
                name: "MergedIntoContactId",
                table: "chat_contacts");

            migrationBuilder.AddColumn<string>(
                name: "GuestToken",
                table: "chat_contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
