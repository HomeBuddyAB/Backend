namespace HomeBuddy_API.DTOs.Requests.GroupDTOs
{
    public class ProductGroupDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ObjectId { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public bool IsDeleted { get; set; }
        public CategoryDto Category { get; set; } = null!;
        /// <summary>True if any variant in this group has ListPrice set (displayed as discount).</summary>
        public bool HasDiscount { get; set; }
    }

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
    }
}
