namespace HomeBuddy_API.DTOs.Requests.ReviewDTOs
{
    public class ReviewUpdateDto
    {
        public int Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}
