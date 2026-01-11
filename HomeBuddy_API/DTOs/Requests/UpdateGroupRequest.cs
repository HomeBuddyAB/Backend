
using System;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests;
public class UpdateGroupRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public Guid CategoryId { get; set; }

    [MaxLength(200)]
    public string? Slug { get; set; }
}
