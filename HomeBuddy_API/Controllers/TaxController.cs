using HomeBuddy_API.Interfaces.TaxInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeBuddy_API.Controllers;

/// <summary>
/// Tax (VAT) endpoints for checkout. European country tax brackets are embedded.
/// Use at checkout: select country of recipient, then apply the returned tax rate to cart total.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TaxController : ControllerBase
{
    private readonly ITaxBracketService _taxService;

    public TaxController(ITaxBracketService taxService)
    {
        _taxService = taxService;
    }

    /// <summary>
    /// Returns all European countries with their VAT rates for checkout country selection.
    /// Frontend: use this list for the country dropdown; apply the selected country's vatRate to cart subtotal.
    /// </summary>
    [HttpGet("countries")]
    public IActionResult GetCountries()
    {
        var countries = _taxService.GetAllCountries();
        return Ok(countries);
    }

    /// <summary>
    /// Calculates tax breakdown for a given subtotal and country.
    /// Use when the frontend has the cart subtotal and wants the exact tax/total before checkout.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g. DE, FR, GB)</param>
    /// <param name="subtotal">Cart subtotal (sum of item prices × quantities)</param>
    [HttpGet("calculate")]
    public IActionResult CalculateTax([FromQuery] string countryCode, [FromQuery] decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return BadRequest(new { error = "countryCode is required" });

        if (subtotal < 0)
            return BadRequest(new { error = "subtotal must be non-negative" });

        var result = _taxService.CalculateTax(subtotal, countryCode);
        if (result == null)
            return NotFound(new { error = $"Country '{countryCode}' is not supported. Use GET /api/tax/countries for valid codes." });

        return Ok(result);
    }
}
