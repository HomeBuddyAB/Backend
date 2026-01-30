using HomeBuddy_API.DTOs.Responses;

namespace HomeBuddy_API.Services;

/// <summary>
/// European country VAT (tax) brackets - standard rates as of 2024.
/// Source: European Commission, Tax Foundation.
/// </summary>
public static class EuropeanTaxBrackets
{
    /// <summary>
    /// All European countries with their ISO 3166-1 alpha-2 code, name, and standard VAT rate (0-100).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, CountryTaxBracket> Countries = new Dictionary<string, CountryTaxBracket>(StringComparer.OrdinalIgnoreCase)
    {
        // EU Member States
        ["AT"] = new("AT", "Austria", 20m),
        ["BE"] = new("BE", "Belgium", 21m),
        ["BG"] = new("BG", "Bulgaria", 20m),
        ["HR"] = new("HR", "Croatia", 25m),
        ["CY"] = new("CY", "Cyprus", 19m),
        ["CZ"] = new("CZ", "Czech Republic", 21m),
        ["DK"] = new("DK", "Denmark", 25m),
        ["EE"] = new("EE", "Estonia", 22m),
        ["FI"] = new("FI", "Finland", 24m),
        ["FR"] = new("FR", "France", 20m),
        ["DE"] = new("DE", "Germany", 19m),
        ["GR"] = new("GR", "Greece", 24m),
        ["HU"] = new("HU", "Hungary", 27m),
        ["IE"] = new("IE", "Ireland", 23m),
        ["IT"] = new("IT", "Italy", 22m),
        ["LV"] = new("LV", "Latvia", 21m),
        ["LT"] = new("LT", "Lithuania", 21m),
        ["LU"] = new("LU", "Luxembourg", 17m),
        ["MT"] = new("MT", "Malta", 18m),
        ["NL"] = new("NL", "Netherlands", 21m),
        ["PL"] = new("PL", "Poland", 23m),
        ["PT"] = new("PT", "Portugal", 23m),
        ["RO"] = new("RO", "Romania", 19m),
        ["SK"] = new("SK", "Slovakia", 20m),
        ["SI"] = new("SI", "Slovenia", 22m),
        ["ES"] = new("ES", "Spain", 21m),
        ["SE"] = new("SE", "Sweden", 25m),

        // Non-EU European
        ["GB"] = new("GB", "United Kingdom", 20m),
        ["NO"] = new("NO", "Norway", 25m),
        ["CH"] = new("CH", "Switzerland", 8.1m),
        ["IS"] = new("IS", "Iceland", 24m),
        ["TR"] = new("TR", "Turkey", 20m),
        ["AL"] = new("AL", "Albania", 20m),
        ["AD"] = new("AD", "Andorra", 4.5m),
        ["BA"] = new("BA", "Bosnia and Herzegovina", 17m),
        ["LI"] = new("LI", "Liechtenstein", 8.1m),
        ["MK"] = new("MK", "North Macedonia", 18m),
        ["ME"] = new("ME", "Montenegro", 21m),
        ["RS"] = new("RS", "Serbia", 20m),
        ["UA"] = new("UA", "Ukraine", 20m),
        ["XK"] = new("XK", "Kosovo", 18m),
        ["SM"] = new("SM", "San Marino", 17m),
        ["MC"] = new("MC", "Monaco", 20m),  // Aligned with France
        ["VA"] = new("VA", "Vatican City", 22m),  // Aligned with Italy
    };

    /// <summary>
    /// Gets the VAT rate for a country by ISO code. Returns null if not found.
    /// </summary>
    public static decimal? GetVatRate(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode)) return null;
        return Countries.TryGetValue(countryCode.Trim().ToUpperInvariant(), out var bracket)
            ? bracket.VatRate
            : null;
    }

    /// <summary>
    /// Gets all countries sorted by name for dropdown/selection.
    /// </summary>
    public static IReadOnlyList<CountryTaxBracket> GetAllCountriesSorted()
    {
        return Countries.Values.OrderBy(c => c.Name).ToList();
    }
}
