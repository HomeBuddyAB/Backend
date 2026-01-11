using HomeBuddy_API.Models;
namespace HomeBuddy_API.Interfaces.ReviewInterfaces
{
    public interface IReviewRepository
    {
        Task<Review?> GetByIdAsync(int id);
        Task<IEnumerable<Review>> GetByProductGroupIdAsync(Guid productGroupId);
        Task<IEnumerable<Review>> GetByProductGroupSlugAsync(string slug);
        Task<IEnumerable<Review>> GetAllAsync(int page);
        Task<int> GetReviewsCountAsync();
        Task CreateAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(int id);
    }
}