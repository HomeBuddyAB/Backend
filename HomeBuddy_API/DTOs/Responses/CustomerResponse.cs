namespace HomeBuddy_API.DTOs.Responses
{
    public class CustomerResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? CountryCode { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
