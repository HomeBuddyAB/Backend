using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishStatusAndCategoryMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportSource",
                table: "ProductGroups",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishStatus",
                table: "ProductGroups",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Published");

            migrationBuilder.AddColumn<string>(
                name: "RawCategoryHint",
                table: "ProductGroups",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoryMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierTerm = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SupplierSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubcategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryMappings_Categories_SubcategoryId",
                        column: x => x.SubcategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryMapping_Term_Source",
                table: "CategoryMappings",
                columns: new[] { "SupplierTerm", "SupplierSource" },
                unique: true,
                filter: "[SupplierSource] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryMappings_SubcategoryId",
                table: "CategoryMappings",
                column: "SubcategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryMappings");

            migrationBuilder.DropColumn(
                name: "ImportSource",
                table: "ProductGroups");

            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "ProductGroups");

            migrationBuilder.DropColumn(
                name: "RawCategoryHint",
                table: "ProductGroups");
        }
    }
}
