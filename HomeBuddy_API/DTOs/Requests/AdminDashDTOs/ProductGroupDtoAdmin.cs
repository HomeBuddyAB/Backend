namespace HomeBuddy_API.DTOs.Requests.AdminDashDTOs
{
    public class ProductGroupDtoAdmin
    {
        public Guid Id { get; set; }
        public string ObjectId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Slug { get; set; }
        public Guid CategoryId { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
