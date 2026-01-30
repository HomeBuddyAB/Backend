namespace HomeBuddy_API.DTOs.Responses;

public record CountryTaxBracket(string Code, string Name, decimal VatRate);

public record TaxCalculationResult(decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total);
