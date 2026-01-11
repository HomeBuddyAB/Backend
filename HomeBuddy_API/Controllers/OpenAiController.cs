namespace HomeBuddy_API.Controllers
{
    //using Azure.AI.OpenAI;
    //using OpenAI.Chat;
    // using System.Text.Json;

    using HomeBuddy_API.Interfaces.ReviewInterfaces;
    using Microsoft.AspNetCore.Mvc;
    using System.Linq;

    public record RatingDistributionDto(int Stars, int Count, double Percentage);
    public record ReviewSummaryDto(
        double AverageRating,
        int TotalReviews,
        List<RatingDistributionDto> RatingDistribution
    );

    [ApiController]
    [Route("api/[controller]")]
    public class OpenAiController : ControllerBase
    {
        //private readonly AzureOpenAIClient _client;
        //private readonly string _deploymentName;
        private readonly IReviewService _reviewService;

        public OpenAiController(/*AzureOpenAIClient client, IConfiguration configuration,*/ IReviewService reviewService)
        {
            //_client = client;
            //_deploymentName = configuration["AZURE_OPENAI_DEPLOYMENT_NAME"] ?? "HomeBuddy";
            _reviewService = reviewService;
        }

        [HttpGet("summarize/{slug}")]
        public async Task<IActionResult> SummarizeProductReviews(string slug)
        {
            var reviews = await _reviewService.GetReviewsByProductGroupSlugAsync(slug);

            if (reviews == null || !reviews.Any())
                return NotFound($"No reviews found for product group: {slug}");

            // --- START: NON-AI STATISTICAL SUMMARY LOGIC (Replaced TEMPORARY FAKE RESPONSE) ---

            int totalReviews = reviews.Count();

            // 1. Calculate Average Rating
            double averageRating = reviews.Average(r => r.Rating);

            // 2. Calculate Rating Distribution
            var ratingDistribution = reviews
                .GroupBy(r => r.Rating)
                .Select(g => new RatingDistributionDto(
                    Stars: g.Key,
                    Count: g.Count(),
                    Percentage: (double)g.Count() / totalReviews // Calculate the percentage
                ))
                .ToList();

            // Ensure all 5 star levels (1-5) are present, even if count is zero
            var existingStars = ratingDistribution.Select(d => d.Stars).ToHashSet();
            for (int i = 1; i <= 5; i++)
            {
                if (!existingStars.Contains(i))
                {
                    ratingDistribution.Add(new RatingDistributionDto(i, 0, 0.0));
                }
            }
            // Sort to ensure presentation order (e.g., 5 down to 1)
            ratingDistribution = ratingDistribution.OrderByDescending(d => d.Stars).ToList();


            // 3. Construct the final summary object
            var reviewSummary = new ReviewSummaryDto(
                AverageRating: Math.Round(averageRating, 2), // Round to 2 decimal places
                TotalReviews: totalReviews,
                RatingDistribution: ratingDistribution
            );

            // Return the calculated summary
            return Ok(reviewSummary);

            // --- END: NON-AI STATISTICAL SUMMARY LOGIC ---


            /*
            // ---------------- REAL AI API CALL (DISABLED TEMPORARILY) ----------------
            var reviewJson = JsonSerializer.Serialize(reviews);

            var messages = new List<MessageDto>
            {
                new MessageDto(
                    "system",
                    "Analyze the provided product reviews and generate a concise summary..."
                ),
                new MessageDto("user", reviewJson)
            };

            var chatClient = _client.GetChatClient(_deploymentName);

            var chatMessages = messages.Select<MessageDto, ChatMessage>(m => m.Role.ToLower() switch
            {
                "system" => new SystemChatMessage(m.Content),
                "user" => new UserChatMessage(m.Content),
                _ => new UserChatMessage(m.Content)
            }).ToList();

            var response = await chatClient.CompleteChatAsync(chatMessages);
            var reply = response.Value.Content[0].Text;

            return Ok(new { chatMessages, reply });
            */
        }

        // The original DTOs for the AI request are no longer needed for this function, 
        // but can be kept commented out or removed based on future plans.
        // public record ChatRequest(List<MessageDto> Messages);
        // public record MessageDto(string Role, string Content);
    }
}