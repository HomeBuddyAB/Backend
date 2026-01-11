using HomeBuddy_API.Data;
using HomeBuddy_API.Extensions;
using HomeBuddy_API.Interfaces.ReviewInterfaces;
using HomeBuddy_API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Review>> GetAllAsync(int page)
        {
            return await _context.Reviews
                .Paginate(page)
                .ToListAsync();
        }

        public async Task<int> GetReviewsCountAsync()
        {
            return await _context.Reviews.CountAsync();
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
        }

        // Updated method
        public async Task<IEnumerable<Review>> GetByProductGroupIdAsync(Guid productGroupId)
        {
            return await _context.Reviews
                .Where(r => r.ProductGroupId == productGroupId)
                .OrderByDescending(r => r.CreatedUtc)
                .ToListAsync();
        }

        // New method - get reviews by slug
        public async Task<IEnumerable<Review>> GetByProductGroupSlugAsync(string slug)
        {
            return await _context.Reviews
                .Include(r => r.ProductGroup)
                .Where(r => r.ProductGroup != null && r.ProductGroup.Slug == slug)
                .OrderByDescending(r => r.CreatedUtc)
                .ToListAsync();
        }

        public async Task UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _context.Reviews
                .Where(r => r.Id == id)
                .ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
        }
    }
}