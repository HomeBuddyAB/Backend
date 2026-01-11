using HomeBuddy_API.DTOs.Requests.ReviewDTOs;
using HomeBuddy_API.Interfaces.ReviewInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,User")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReview(int id)
    {
        var review = await _reviewService.GetReviewAsync(id);
        if (review == null) return NotFound();
        return Ok(review);
    }

    // New endpoint - Get reviews by ProductGroup slug
    [HttpGet("product-group/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewsBySlug(string slug)
    {
        var reviews = await _reviewService.GetReviewsByProductGroupSlugAsync(slug);
        if (reviews == null || !reviews.Any())
            return NotFound("This product group has no reviews");
        return Ok(reviews);
    }

    // Updated endpoint - Get reviews by ProductGroup ID
    [HttpGet("product-group/id/{productGroupId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewsByProductGroupId(Guid productGroupId)
    {
        var reviews = await _reviewService.GetReviewsByProductGroupIdAsync(productGroupId);
        if (reviews == null || !reviews.Any())
            return NotFound("This product group has no reviews");
        return Ok(reviews);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReviews(int page)
    {
        var reviews = await _reviewService.GetAllReviewsAsync(page);
        if (reviews == null) return NotFound("There are no reviews registered");
        return Ok(reviews);
    }

    [HttpGet("count")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetReviewCount()
    {
        var count = await _reviewService.GetReviewsCountAsync();
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto dto)
    {
        await _reviewService.CreateReviewAsync(dto);
        return Ok("Review created");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, ReviewUpdateDto dto)
    {
        await _reviewService.UpdateReviewAsync(id, dto);
        return Ok("Updated");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        await _reviewService.DeleteReviewAsync(id);
        return Ok("Deleted");
    }
}