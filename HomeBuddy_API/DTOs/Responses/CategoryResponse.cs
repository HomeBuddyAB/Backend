
using System;

namespace HomeBuddy_API.DTOs.Responses;
public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public string? ParentCategorySlug { get; set; }
    public int SubcategoryCount { get; set; }
    public int ProductGroupCount { get; set; }
}
