
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeBuddy_API.Data;

namespace HomeBuddy_API.Controllers.Public;

[ApiController]
[Route("variants")]
public class PublicVariantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public PublicVariantsController(ApplicationDbContext db) { _db = db; }

    // /variants/{sku} -> redirect to /groups/{slug or objectId}?sku={sku}
    [HttpGet("{sku}")]
    public async Task<IActionResult> RedirectToGroup(string sku, CancellationToken ct)
    {
        var v = await _db.Variants.Include(x => x.ProductGroup).FirstOrDefaultAsync(x => x.Sku == sku && !x.IsDeleted && !x.ProductGroup.IsDeleted, ct);
        if (v == null) return NotFound();
        var slugOrObject = string.IsNullOrWhiteSpace(v.ProductGroup.Slug) ? v.ProductGroup.ObjectId : v.ProductGroup.Slug!;
        var url = $"/groups/{slugOrObject}?sku={Uri.EscapeDataString(sku)}";
        return Redirect(url);
    }
}
