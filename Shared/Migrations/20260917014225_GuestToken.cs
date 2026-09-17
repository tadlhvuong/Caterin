using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class GuestToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestToken",
                table: "chat_contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestToken",
                table: "chat_contacts");
        }
    }
}
