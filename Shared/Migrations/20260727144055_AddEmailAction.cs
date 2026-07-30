using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailActions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailActions_ExpiredAt",
                table: "EmailActions",
                column: "ExpiredAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailActions_KeyHash",
                table: "EmailActions",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailActions_RevokedAt",
                table: "EmailActions",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailActions_UsedAt",
                table: "EmailActions",
                column: "UsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailActions_UserId_Type",
                table: "EmailActions",
                columns: new[] { "UserId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailActions");
        }
    }
}
