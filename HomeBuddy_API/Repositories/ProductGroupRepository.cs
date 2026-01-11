// HomeBuddy_API/Repositories/ProductGroupRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class ProductGroupRepository : IProductGroupRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductGroupRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Check if any group has the specified ObjectId
        public async Task<bool> ExistsByObjectIdAsync(string objectId, CancellationToken ct = default)
        {
            return await _dbContext.ProductGroups
                                   .AsNoTracking()
                                   .AnyAsync(pg => pg.ObjectId == objectId, ct);
        }

        // Get a single group by slug OR objectId (public-facing)
        public async Task<ProductGroup?> GetBySlugOrObjectIdAsync(string idOrSlug, CancellationToken ct = default)
        {
            return await _dbContext.ProductGroups
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(pg => !pg.IsDeleted &&
                                                              (pg.Slug == idOrSlug || pg.ObjectId == idOrSlug), ct);
        }

        // Admin-only: get group by slug OR objectId, including soft-deleted
        public async Task<ProductGroup?> GetBySlugOrObjectIdIncludingDeletedAsync(string idOrSlug, CancellationToken ct = default)
        {
            return await _dbContext.ProductGroups
                                   .AsNoTracking()
                                   .FirstOrDefaultAsync(pg => pg.Slug == idOrSlug || pg.ObjectId == idOrSlug, ct);
        }

        // Optionally add create/update methods if you plan to modify groups
        public async Task AddAsync(ProductGroup group, CancellationToken ct = default)
        {
            await _dbContext.ProductGroups.AddAsync(group, ct);
        }

        public void Update(ProductGroup group)
        {
            _dbContext.ProductGroups.Update(group);
        }

        // List all active groups (filtered by category if needed)
        public async Task<List<ProductGroup>> ListActiveGroupsAsync(Guid? categoryId = null, CancellationToken ct = default)
        {
            var query = _dbContext.ProductGroups.AsNoTracking().Where(pg => !pg.IsDeleted);
            if (categoryId.HasValue)
                query = query.Where(pg => pg.CategoryId == categoryId.Value);
            return await query.ToListAsync(ct);
        }
    }
}
