using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeBuddy_API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // DbSets
        public DbSet<Admin> Admins { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderItem> OrderItems { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<ProductCategory> ProductCategories { get; set; } = default!;
        public DbSet<ProductImage> ProductImages { get; set; } = default!;
        public DbSet<Review> Reviews { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<ProductGroup> ProductGroups => Set<ProductGroup>();
        public DbSet<Variant> Variants { get; set; } = default!;
        public DbSet<Inventory> Inventories { get; set; } = default!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = default!;
        public DbSet<ColorImage> ColorImages => Set<ColorImage>();
        public DbSet<VariantImage> VariantImages => Set<VariantImage>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Change the lambda to avoid returning null, since the property is required and should not be null.
            // Instead, return string.Empty if v is null.
            var skuConverter = new ValueConverter<string, string>(
                v => string.IsNullOrWhiteSpace(v) ? string.Empty : v.Trim().ToUpperInvariant(),
                v => v
            );

            modelBuilder.Entity<Variant>(b =>
            {
                b.Property(v => v.Sku)
                 .HasConversion(skuConverter)
                 .IsRequired()
                 .HasMaxLength(100);

                // Enforce DB-level uniqueness for SKU
                b.HasIndex(v => v.Sku)
                 .IsUnique()
                 .HasDatabaseName("IX_Variant_Sku");
            });

            modelBuilder.Entity<ProductGroup>()
                .HasIndex(g => g.ObjectId).IsUnique();

            modelBuilder.Entity<ProductGroup>()
                .HasOne(g => g.Category)
                .WithMany(c => c.ProductGroups)
                .HasForeignKey(g => g.CategoryId);

            modelBuilder.Entity<Variant>()
                .HasOne(v => v.ProductGroup)
                .WithMany(g => g.Variants)
                .HasForeignKey(v => v.ProductGroupId);

            modelBuilder.Entity<Inventory>(b =>
            {
                b.HasOne(i => i.Variant)
                 .WithOne(v => v.Inventory)
                 .HasForeignKey<Inventory>(i => i.VariantId);

                // Concurrency token (requires Inventory.RowVersion byte[] in model with [Timestamp])
                b.Property(i => i.RowVersion)
                 .IsRowVersion()
                 .IsConcurrencyToken();
            });

            modelBuilder.Entity<ColorImage>()
                .HasOne(ci => ci.ProductGroup)
                .WithMany(pg => pg.ColorImages)
                .HasForeignKey(ci => ci.ProductGroupId);

            modelBuilder.Entity<VariantImage>()
                .HasOne(vi => vi.Variant)
                .WithMany(v => v.VariantImages)
                .HasForeignKey(vi => vi.VariantId);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNo)
                .IsUnique();

            // Order ↔ OrderItem (1:N)
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<InventoryTransaction>(b =>
            {
                b.HasKey(t => t.Id);

                b.HasOne(t => t.Inventory)
                 .WithMany(i => i.Transactions)
                 .HasForeignKey(t => t.InventoryId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(t => new { t.InventoryId, t.TransactionType, t.ReferenceId })
                 .HasDatabaseName("IX_InventoryTransaction_InventoryId_Type_Ref");
            });


            // Keep or add explicit relationship configuration if you have Inventory/Variant one-to-one
            modelBuilder.Entity<Variant>()
                .HasOne(v => v.Inventory)
                .WithOne(i => i.Variant)
                .HasForeignKey<Inventory>(i => i.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // ...other model config
        }
    }
}
