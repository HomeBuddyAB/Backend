
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Slug { get; set; } = null!;

    public ICollection<ProductGroup> ProductGroups { get; set; } = new List<ProductGroup>();
}
