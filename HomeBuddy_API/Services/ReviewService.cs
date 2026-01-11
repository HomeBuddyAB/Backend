using HomeBuddy_API.DTOs.Requests.ReviewDTOs;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Interfaces.ReviewInterfaces;
using HomeBuddy_API.Models;

namespace HomeBuddy_API.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly IProductGroupRepository _productGroupRepo;

        public ReviewService(IReviewRepository reviewRepo, IProductGroupRepository productGroupRepo)
        {
            _reviewRepo = reviewRepo;
            _productGroupRepo = productGroupRepo;
        }

        public async Task<IEnumerable<Review>> GetAllReviewsAsync(int page)
        {
            return await _reviewRepo.GetAllAsync(page);
        }

        public async Task<int> GetReviewsCountAsync()
        {
            return await _reviewRepo.GetReviewsCountAsync();
        }

        public async Task<Review?> GetReviewAsync(int id)
        {
            return await _reviewRepo.GetByIdAsync(id);
        }

        // Updated method
        public async Task<IEnumerable<Review>> GetReviewsByProductGroupIdAsync(Guid productGroupId)
        {
            return await _reviewRepo.GetByProductGroupIdAsync(productGroupId);
        }

        // New method
        public async Task<IEnumerable<Review>> GetReviewsByProductGroupSlugAsync(string slug)
        {
            return await _reviewRepo.GetByProductGroupSlugAsync(slug);
        }

        public async Task CreateReviewAsync(ReviewCreateDto dto)
        {
            var productGroup = await _productGroupRepo.GetBySlugOrObjectIdAsync(dto.ProductGroupSlug);

            if (productGroup == null)
            {
                throw new KeyNotFoundException($"Product Group with slug '{dto.ProductGroupSlug}' was not found.");
            }

            var review = new Review
            {
                ProductGroupId = productGroup.Id,
                Rating = dto.Rating,
                Title = dto.Title,
                Comment = dto.Comment,
                CreatedUtc = DateTime.UtcNow
            };

            await _reviewRepo.CreateAsync(review);
        }

        public async Task UpdateReviewAsync(int id, ReviewUpdateDto dto)
        {
            var existingReview = await _reviewRepo.GetByIdAsync(id);
            if (existingReview == null)
                throw new Exception("Review not found");

            if (existingReview.Rating != dto.Rating)
                existingReview.Rating = dto.Rating;
            if (!string.IsNullOrWhiteSpace(dto.Comment))
                existingReview.Comment = dto.Comment;
            if (!string.IsNullOrEmpty(dto.Title))
                existingReview.Title = dto.Title;

            existingReview.UpdatedUtc = DateTime.UtcNow;
            await _reviewRepo.UpdateAsync(existingReview);
        }

        public async Task DeleteReviewAsync(int id)
        {
            var existingReview = await _reviewRepo.GetByIdAsync(id);
            if (existingReview == null)
                throw new Exception("Review not found");

            await _reviewRepo.DeleteAsync(id);
        }
    }
}