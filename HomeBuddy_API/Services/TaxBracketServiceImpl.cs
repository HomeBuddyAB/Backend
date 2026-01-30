using HomeBuddy_API.DTOs.Responses;
using HomeBuddy_API.Interfaces.TaxInterfaces;

namespace HomeBuddy_API.Services;

public class TaxBracketServiceImpl : ITaxBracketService
{
    public IReadOnlyList<CountryTaxBracket> GetAllCountries()
    {
        return EuropeanTaxBrackets.GetAllCountriesSorted();
    }

    public decimal? GetVatRate(string countryCode)
    {
        return EuropeanTaxBrackets.GetVatRate(countryCode);
    }

    public TaxCalculationResult? CalculateTax(decimal subtotal, string countryCode)
    {
        var rate = EuropeanTaxBrackets.GetVatRate(countryCode);
        if (rate == null) return null;

        var taxAmount = Math.Round(subtotal * (rate.Value / 100m), 2, MidpointRounding.AwayFromZero);
        var total = subtotal + taxAmount;

        return new TaxCalculationResult(subtotal, rate.Value, taxAmount, total);
    }
}
