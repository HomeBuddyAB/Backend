namespace HomeBuddy_API.Services.Import;

/// <summary>
/// Hardcoded test adapter that produces sample import data.
/// Simulates what a real supplier feed would look like after parsing.
/// Includes clean items, items with unknown categories, and incomplete items.
/// </summary>
public static class TestDataAdapter
{
    public const string SourceName = "test-adapter";

    public static List<NormalizedRow> GetTestData()
    {
        return new List<NormalizedRow>
        {
            // Clean item — known category hint, complete data
            new()
            {
                ExternalId = "TEST-SHELF-001",
                Name = "Pine Wall Shelf",
                RawCategoryHint = "Furniture",
                Variants = new()
                {
                    new() { Sku = "TEST-SHELF-NAT-60", Color = "Natural", Size = "60cm", Price = 29.99m, Stock = 25, Brand = "HomeCraft", Material = "Pine", ImageUrls = new() { "https://placehold.co/400x400/e8d5b7/333?text=Pine+Shelf" } },
                    new() { Sku = "TEST-SHELF-WHT-60", Color = "White", Size = "60cm", Price = 34.99m, Stock = 18, Brand = "HomeCraft", Material = "Pine", ImageUrls = new() { "https://placehold.co/400x400/ffffff/333?text=White+Shelf" } },
                    new() { Sku = "TEST-SHELF-NAT-90", Color = "Natural", Size = "90cm", Price = 44.99m, Stock = 12, Brand = "HomeCraft", Material = "Pine", ImageUrls = new() { "https://placehold.co/400x400/e8d5b7/333?text=Pine+Shelf+90" } },
                }
            },

            // Clean item — different known category
            new()
            {
                ExternalId = "TEST-LAMP-001",
                Name = "Industrial Pendant Light",
                RawCategoryHint = "Lighting",
                Variants = new()
                {
                    new() { Sku = "TEST-LAMP-BLK-SM", Color = "Black", Size = "Small", Price = 59.99m, ListPrice = 79.99m, Stock = 30, Brand = "LuxLight", Material = "Metal", ImageUrls = new() { "https://placehold.co/400x400/1a1a1a/fff?text=Pendant+Black" } },
                    new() { Sku = "TEST-LAMP-BRS-SM", Color = "Brass", Size = "Small", Price = 69.99m, Stock = 15, Brand = "LuxLight", Material = "Brass", ImageUrls = new() { "https://placehold.co/400x400/b5a642/fff?text=Pendant+Brass" } },
                }
            },

            // Unknown category — should go to Uncategorized
            new()
            {
                ExternalId = "TEST-TILE-001",
                Name = "Ceramic Floor Tile",
                RawCategoryHint = "Flooring",
                Variants = new()
                {
                    new() { Sku = "TEST-TILE-GRY-30", Color = "Grey", Size = "30x30cm", Price = 12.99m, Stock = 200, Brand = "TileCo", Material = "Ceramic", ImageUrls = new() { "https://placehold.co/400x400/999/fff?text=Grey+Tile" } },
                    new() { Sku = "TEST-TILE-WHT-30", Color = "White", Size = "30x30cm", Price = 14.99m, Stock = 150, Brand = "TileCo", Material = "Ceramic", ImageUrls = new() { "https://placehold.co/400x400/eee/333?text=White+Tile" } },
                }
            },

            // Missing price — should be flagged
            new()
            {
                ExternalId = "TEST-DRILL-001",
                Name = "Cordless Impact Drill",
                RawCategoryHint = "Power Tools",
                Variants = new()
                {
                    new() { Sku = "TEST-DRILL-18V", Color = "Yellow", Size = "18V", Price = 0m, Stock = 8, Brand = "PowerMax", Material = "Plastic/Metal", ImageUrls = new() { "https://placehold.co/400x400/ffcc00/333?text=Drill" } },
                }
            },

            // Missing image — should be flagged
            new()
            {
                ExternalId = "TEST-SCREW-001",
                Name = "Stainless Steel Screw Pack",
                RawCategoryHint = "Materials",
                Variants = new()
                {
                    new() { Sku = "TEST-SCREW-M4-50", Color = "Silver", Size = "M4x50mm", Price = 6.99m, Stock = 500, Brand = "FixIt", Material = "Stainless Steel" },
                }
            },

            // No category hint at all
            new()
            {
                ExternalId = "TEST-MISC-001",
                Name = "Mystery Clearance Item",
                RawCategoryHint = null,
                Variants = new()
                {
                    new() { Sku = "TEST-MISC-001-A", Color = "Assorted", Size = "One Size", Price = 9.99m, Stock = 3, ImageUrls = new() { "https://placehold.co/400x400/ff6b6b/fff?text=Mystery" } },
                }
            },

            // Zero stock — warning only, not a blocker
            new()
            {
                ExternalId = "TEST-PAINT-001",
                Name = "Eco Wall Paint",
                RawCategoryHint = "Materials",
                Variants = new()
                {
                    new() { Sku = "TEST-PAINT-WHT-5L", Color = "White", Size = "5L", Price = 39.99m, Stock = 0, Brand = "GreenCoat", Material = "Water-based", ImageUrls = new() { "https://placehold.co/400x400/f0f0f0/333?text=White+Paint" } },
                    new() { Sku = "TEST-PAINT-GRY-5L", Color = "Grey", Size = "5L", Price = 42.99m, Stock = 0, Brand = "GreenCoat", Material = "Water-based", ImageUrls = new() { "https://placehold.co/400x400/888/fff?text=Grey+Paint" } },
                }
            },
        };
    }
}
