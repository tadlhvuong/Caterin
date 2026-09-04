using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class addStockProdcut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_MediaAlbums_AlbumId",
                table: "MediaFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedia_MediaFiles_MediaFileId",
                table: "ProductMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedia_Products_ProductId",
                table: "ProductMedia");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_AlbumId",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_FileName",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_Type",
                table: "MediaFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMedia",
                table: "ProductMedia");

            migrationBuilder.DropIndex(
                name: "IX_ProductMedia_ProductId_MediaFileId",
                table: "ProductMedia");

            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "MediaFiles");

            migrationBuilder.RenameTable(
                name: "ProductMedia",
                newName: "ProductMedias");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "MediaFiles",
                newName: "MediaAlbumId");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                table: "MediaFiles",
                newName: "Size");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "MediaFiles",
                newName: "StoragePath");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedia_ProductId_IsPrimary",
                table: "ProductMedias",
                newName: "IX_ProductMedias_ProductId_IsPrimary");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedia_ProductId_DisplayOrder",
                table: "ProductMedias",
                newName: "IX_ProductMedias_ProductId_DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedia_MediaFileId",
                table: "ProductMedias",
                newName: "IX_ProductMedias_MediaFileId");

            migrationBuilder.AddColumn<decimal>(
                name: "Stock",
                table: "Products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MediaFiles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "MediaFiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "MediaFiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "ProductMedias",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "ProductMedias",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMedias",
                table: "ProductMedias",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductVariantMedias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductVariantId = table.Column<int>(type: "integer", nullable: false),
                    AttributeValueId = table.Column<int>(type: "integer", nullable: false),
                    MediaFileId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantMedias_AttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalTable: "AttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantMedias_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductVariantMedias_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_MediaAlbumId",
                table: "MediaFiles",
                column: "MediaAlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMedias_AttributeValueId",
                table: "ProductVariantMedias",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMedias_MediaFileId",
                table: "ProductVariantMedias",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMedias_ProductVariantId_AttributeValueId_Medi~",
                table: "ProductVariantMedias",
                columns: new[] { "ProductVariantId", "AttributeValueId", "MediaFileId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_MediaAlbums_MediaAlbumId",
                table: "MediaFiles",
                column: "MediaAlbumId",
                principalTable: "MediaAlbums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedias_MediaFiles_MediaFileId",
                table: "ProductMedias",
                column: "MediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedias_Products_ProductId",
                table: "ProductMedias",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaFiles_MediaAlbums_MediaAlbumId",
                table: "MediaFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedias_MediaFiles_MediaFileId",
                table: "ProductMedias");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductMedias_Products_ProductId",
                table: "ProductMedias");

            migrationBuilder.DropTable(
                name: "ProductVariantMedias");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_MediaAlbumId",
                table: "MediaFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMedias",
                table: "ProductMedias");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "MediaFiles");

            migrationBuilder.RenameTable(
                name: "ProductMedias",
                newName: "ProductMedia");

            migrationBuilder.RenameColumn(
                name: "StoragePath",
                table: "MediaFiles",
                newName: "FilePath");

            migrationBuilder.RenameColumn(
                name: "Size",
                table: "MediaFiles",
                newName: "FileSize");

            migrationBuilder.RenameColumn(
                name: "MediaAlbumId",
                table: "MediaFiles",
                newName: "Width");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedias_ProductId_IsPrimary",
                table: "ProductMedia",
                newName: "IX_ProductMedia_ProductId_IsPrimary");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedias_ProductId_DisplayOrder",
                table: "ProductMedia",
                newName: "IX_ProductMedia_ProductId_DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "IX_ProductMedias_MediaFileId",
                table: "ProductMedia",
                newName: "IX_ProductMedia_MediaFileId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MediaFiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ContentType",
                table: "MediaFiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "AlbumId",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "MediaFiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "MediaFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MediaFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "MediaFiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrimary",
                table: "ProductMedia",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "DisplayOrder",
                table: "ProductMedia",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMedia",
                table: "ProductMedia",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_AlbumId",
                table: "MediaFiles",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_FileName",
                table: "MediaFiles",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Type",
                table: "MediaFiles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId_MediaFileId",
                table: "ProductMedia",
                columns: new[] { "ProductId", "MediaFileId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MediaFiles_MediaAlbums_AlbumId",
                table: "MediaFiles",
                column: "AlbumId",
                principalTable: "MediaAlbums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedia_MediaFiles_MediaFileId",
                table: "ProductMedia",
                column: "MediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductMedia_Products_ProductId",
                table: "ProductMedia",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
