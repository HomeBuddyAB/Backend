namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>Optional JSON cart from guest session, e.g. {"items":[{"sku":"ABC-123","quantity":2}]}. Becomes user cart on registration.</summary>
        public string? GuestCart { get; set; }
    }
}

// M.B