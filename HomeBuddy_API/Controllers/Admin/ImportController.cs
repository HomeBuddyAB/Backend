using System.ComponentModel.DataAnnotations;
using HomeBuddy_API.Data;
using HomeBuddy_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeBuddy_API.Controllers.Admin;

[ApiController]
[Route("api/admin/import")]
public class ImportController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ImportController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("products")]
    public async Task<IActionResult> ImportProducts([FromBody] ProductImportRequest request, CancellationToken ct)
    {
        // Simple shared-secret check, matching TestApi usage (X-Integration-Key header).
        var expectedKey = _config["Import:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Import:ApiKey is not configured in appsettings.");
        }

        if (!Request.Headers.TryGetValue("X-Integration-Key", out var provided) ||
            !string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal))
        {
            return Unauthorized("Invalid integration key.");
        }

        if (request.Groups == null || request.Groups.Count == 0)
        {
            return BadRequest("At least one group is required.");
        }

        var createdGroups = new List<object>();
        var errors = new List<object>();

        for (var groupIndex = 0; groupIndex < request.Groups.Count; groupIndex++)
        {
            var groupReq = request.Groups[groupIndex];

            // Basic group-level validation
            if (string.IsNullOrWhiteSpace(groupReq.ObjectId))
            {
                errors.Add(new
                {
                    level = "group",
                    index = groupIndex,
                    code = "InvalidObjectId",
                    message = "Each group must have a non-empty objectId."
                });
                continue;
            }

            if (groupReq.ObjectId.Length > 100)
            {
                errors.Add(new
                {
                    level = "group",
                    index = groupIndex,
                    code = "ObjectIdTooLong",
                    message = "ObjectId must be at most 100 characters."
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(groupReq.Name))
            {
                errors.Add(new
                {
                    level = "group",
                    index = groupIndex,
                    code = "InvalidName",
                    message = "Each group must have a non-empty name."
                });
                continue;
            }

            if (groupReq.Name.Length > 200)
            {
                errors.Add(new
                {
                    level = "group",
                    index = groupIndex,
                    code = "NameTooLong",
                    message = "Group name must be at most 200 characters."
                });
                continue;
            }

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == groupReq.CategoryId, ct);
            if (category == null)
            {
                errors.Add(new
                {
                    level = "group",
                    index = groupIndex,
                    code = "CategoryNotFound",
                    message = $"Category not found: {groupReq.CategoryId}"
                });
                continue;
            }

            // Either update existing group (by ObjectId) or create new
            var group = await _db.ProductGroups
                .Include(g => g.Variants)
                .FirstOrDefaultAsync(g => g.ObjectId == groupReq.ObjectId, ct);

            if (group == null)
            {
                group = new ProductGroup
                {
                    Id = Guid.NewGuid(),
                    ObjectId = groupReq.ObjectId,
                    Name = groupReq.Name,
                    CategoryId = groupReq.CategoryId,
                    Slug = GenerateSlug(groupReq.Name)
                };

                await _db.ProductGroups.AddAsync(group, ct);
            }
            else
            {
                group.Name = groupReq.Name;
                group.CategoryId = groupReq.CategoryId;
                group.Slug = GenerateSlug(groupReq.Name);
                group.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (groupReq.Variants != null && groupReq.Variants.Count > 0)
            {
                for (var variantIndex = 0; variantIndex < groupReq.Variants.Count; variantIndex++)
                {
                    var variantReq = groupReq.Variants[variantIndex];

                    // Variant-level validation and guarding
                    if (string.IsNullOrWhiteSpace(variantReq.Sku))
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "InvalidSku",
                            message = "Each variant must have a non-empty SKU."
                        });
                        continue;
                    }

                    if (variantReq.Sku.Length > 100)
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "SkuTooLong",
                            message = "SKU must be at most 100 characters."
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(variantReq.Color))
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "InvalidColor",
                            message = "Color is required for each variant."
                        });
                        continue;
                    }

                    if (variantReq.Color.Length > 60)
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "ColorTooLong",
                            message = "Color must be at most 60 characters."
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(variantReq.Size))
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "InvalidSize",
                            message = "Size is required for each variant."
                        });
                        continue;
                    }

                    if (variantReq.Size.Length > 60)
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "SizeTooLong",
                            message = "Size must be at most 60 characters."
                        });
                        continue;
                    }

                    if (variantReq.Price <= 0)
                    {
                        errors.Add(new
                        {
                            level = "variant",
                            groupIndex,
                            variantIndex,
                            code = "InvalidPrice",
                            message = "Price must be greater than zero."
                        });
                        continue;
                    }

                    var existingVariant = await _db.Variants
                        .FirstOrDefaultAsync(v => v.Sku == variantReq.Sku, ct);

                    if (existingVariant == null)
                    {
                        existingVariant = new Variant
                        {
                            Id = Guid.NewGuid(),
                            ProductGroupId = group.Id,
                            Sku = variantReq.Sku,
                            Color = variantReq.Color!,
                            Size = variantReq.Size!,
                            Price = variantReq.Price,
                            Brand = variantReq.Brand,
                            Material = variantReq.Material
                        };
                        await _db.Variants.AddAsync(existingVariant, ct);
                    }
                    else
                    {
                        existingVariant.ProductGroupId = group.Id;
                        existingVariant.Color = variantReq.Color!;
                        existingVariant.Size = variantReq.Size!;
                        existingVariant.Price = variantReq.Price;
                        existingVariant.Brand = variantReq.Brand;
                        existingVariant.Material = variantReq.Material;
                    }

                    // Ensure inventory record reflects supplier stock
                    if (variantReq.AvailableInStock.HasValue)
                    {
                        var inventory = await _db.Inventories
                            .FirstOrDefaultAsync(i => i.VariantId == existingVariant.Id, ct);

                        if (inventory == null)
                        {
                            inventory = new Inventory
                            {
                                VariantId = existingVariant.Id,
                                Quantity = variantReq.AvailableInStock.Value,
                                LowStockThreshold = 0,
                                LastRestockDate = DateTimeOffset.UtcNow
                            };
                            await _db.Inventories.AddAsync(inventory, ct);
                        }
                        else
                        {
                            inventory.Quantity = variantReq.AvailableInStock.Value;
                            inventory.LastRestockDate = DateTimeOffset.UtcNow;
                        }
                    }

                    // Optional: create/update color-level images for the group based on variant images
                    if (variantReq.ImageUrls != null && variantReq.ImageUrls.Count > 0)
                    {
                        var sortOrder = 0;
                        foreach (var url in variantReq.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
                        {
                            // Avoid duplicates for same group/color/url
                            var exists = await _db.ColorImages.AnyAsync(ci =>
                                    ci.ProductGroupId == group.Id &&
                                    ci.Color == variantReq.Color &&
                                    ci.Url == url,
                                ct);
                            if (exists) continue;

                            var image = new ColorImage
                            {
                                ProductGroupId = group.Id,
                                Color = variantReq.Color!,
                                Url = url,
                                AltText = group.Name,
                                IsPrimary = sortOrder == 0,
                                SortOrder = sortOrder++
                            };
                            await _db.ColorImages.AddAsync(image, ct);
                        }
                    }
                }
            }

            createdGroups.Add(new
            {
                group.ObjectId,
                group.Name,
                group.CategoryId
            });
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            importedGroups = createdGroups.Count,
            groups = createdGroups,
            errors
        });
    }

    private static string GenerateSlug(string input)
    {
        var s = new string(input.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .ToArray())
            .Replace(' ', '-');

        return s;
    }
}

public class ProductImportRequest
{
    [Required]
    public List<ProductImportGroupRequest> Groups { get; set; } = new();
}

public class ProductImportGroupRequest
{
    [Required]
    public string ObjectId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    public List<ProductImportVariantRequest>? Variants { get; set; }
}

public class ProductImportVariantRequest
{
    [Required]
    public string Sku { get; set; } = string.Empty;

    public string? Color { get; set; }
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public string? Brand { get; set; }
    public string? Material { get; set; }

    /// <summary>
    /// Optional current on-hand quantity from the supplier. If provided,
    /// the import will create or update the Inventory row for this variant.
    /// </summary>
    public int? AvailableInStock { get; set; }

    /// <summary>
    /// Optional list of image URLs for this variant's color. These will be stored as ColorImages for the group.
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();
}

