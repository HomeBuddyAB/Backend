using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests;

public class UpdateCategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Slug { get; set; }

    public Guid? ParentCategoryId { get; set; }
}
