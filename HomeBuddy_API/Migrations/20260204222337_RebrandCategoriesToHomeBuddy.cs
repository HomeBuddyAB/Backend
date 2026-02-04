using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeBuddy_API.Migrations
{
    /// <inheritdoc />
    public partial class RebrandCategoriesToHomeBuddy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing clothing categories to HomeBuddy IKEA-like categories
            // Tops -> Furniture, Bottoms -> Materials, Shoes -> Power Tools, Accessories -> Lighting
            migrationBuilder.Sql(@"
                UPDATE Categories SET Name = 'Furniture', Slug = 'furniture' WHERE Slug = 'tops';
                UPDATE Categories SET Name = 'Materials', Slug = 'materials' WHERE Slug = 'bottoms';
                UPDATE Categories SET Name = 'Power Tools', Slug = 'power-tools' WHERE Slug = 'shoes';
                UPDATE Categories SET Name = 'Lighting', Slug = 'lighting' WHERE Slug = 'accessories';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Categories SET Name = 'Tops', Slug = 'tops' WHERE Slug = 'furniture';
                UPDATE Categories SET Name = 'Bottoms', Slug = 'bottoms' WHERE Slug = 'materials';
                UPDATE Categories SET Name = 'Shoes', Slug = 'shoes' WHERE Slug = 'power-tools';
                UPDATE Categories SET Name = 'Accessories', Slug = 'accessories' WHERE Slug = 'lighting';
            ");
        }
    }
}
