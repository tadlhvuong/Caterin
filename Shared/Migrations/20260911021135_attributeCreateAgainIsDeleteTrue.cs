using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class attributeCreateAgainIsDeleteTrue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attributes_Code",
                table: "Attributes");

            migrationBuilder.CreateIndex(
                name: "IX_Attributes_Code",
                table: "Attributes",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attributes_Code",
                table: "Attributes");

            migrationBuilder.CreateIndex(
                name: "IX_Attributes_Code",
                table: "Attributes",
                column: "Code",
                unique: true);
        }
    }
}
