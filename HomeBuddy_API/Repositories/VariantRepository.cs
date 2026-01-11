// HomeBuddy_API/Repositories/VariantRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Models;
using HomeBuddy_API.Data;

namespace HomeBuddy_API.Repositories
{
    public class VariantRepository : IVariantRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public VariantRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Create: adds to the DbContext (does not call SaveChanges; caller/service should SaveChangesAsync)
        public async Task AddAsync(Variant variant, CancellationToken ct = default)
        {
            await _dbContext.Variants.AddAsync(variant, ct);
        }

        // Update: attach/modify an entity (does not call SaveChanges)
        public void Update(Variant variant)
        {
            _dbContext.Variants.Update(variant);
        }

        // Get a variant by SKU (read-only)
        public async Task<Variant?> GetBySkuAsync(string sku, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(v => v.Sku == sku && !v.IsDeleted, ct);
        }

        // Get a variant by SKU with its Inventory attached for modification (tracked)
        public async Task<Variant?> GetBySkuWithInventoryAsync(string sku, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .Include(v => v.Inventory)
                                   .FirstOrDefaultAsync(v => v.Sku == sku && !v.IsDeleted, ct);
        }

        // If you need a read-only SKU+Inventory, call this instead
        public async Task<Variant?> GetBySkuWithInventoryReadOnlyAsync(string sku, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .Include(v => v.Inventory)
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(v => v.Sku == sku && !v.IsDeleted, ct);
        }

        // List all active variants in a product group (read-only)
        public async Task<List<Variant>> ListByGroupAsync(Guid productGroupId, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .AsNoTracking()
                                   .Where(v => v.ProductGroupId == productGroupId && !v.IsDeleted)
                                   .ToListAsync(ct);
        }

        // Admin helpers: include deleted
        public async Task<Variant?> GetBySkuIncludingDeletedAsync(string sku, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(v => v.Sku == sku, ct);
        }

        public async Task<List<Variant>> ListByGroupIncludingDeletedAsync(Guid productGroupId, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                   .AsNoTracking()
                                   .Where(v => v.ProductGroupId == productGroupId)
                                   .ToListAsync(ct);
        }

        // Non-nullable overload required by the interface
        public async Task<Variant?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            return await _dbContext.Variants
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, ct);
        }

        // Nullable overload (keeps callers that pass Guid? safe)
        public async Task<Variant?> GetByIdAsync(Guid? id, CancellationToken ct = default)
        {
            if (!id.HasValue)
                return null;

            return await _dbContext.Variants
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(v => v.Id == id.Value && !v.IsDeleted, ct);
        }
    }
}

