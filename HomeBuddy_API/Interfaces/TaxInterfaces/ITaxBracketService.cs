using HomeBuddy_API.DTOs.Responses;

namespace HomeBuddy_API.Interfaces.TaxInterfaces;

public interface ITaxBracketService
{
    /// <summary>
    /// Returns all European countries with their tax brackets, sorted by name.
    /// </summary>
    IReadOnlyList<CountryTaxBracket> GetAllCountries();

    /// <summary>
    /// Gets the VAT rate for a country. Returns null if country is not supported.
    /// </summary>
    decimal? GetVatRate(string countryCode);

    /// <summary>
    /// Calculates tax breakdown for a given subtotal and country.
    /// </summary>
    TaxCalculationResult? CalculateTax(decimal subtotal, string countryCode);
}
