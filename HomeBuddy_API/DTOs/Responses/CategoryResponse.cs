
using System;

namespace HomeBuddy_API.DTOs.Responses;
public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
}
