using System;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests;

public class UpdateVariantRequest
{
    [Required, MaxLength(60)]
    public string Color { get; set; } = null!;

    [Required, MaxLength(60)]
    public string Size { get; set; } = null!;

    [Range(0, 9999999)]
    public decimal Price { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(200)]
    public string? Material { get; set; }
}