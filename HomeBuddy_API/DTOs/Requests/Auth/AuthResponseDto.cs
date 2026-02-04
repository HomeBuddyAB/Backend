namespace HomeBuddy_API.DTOs.Requests.Auth
{
    public class AuthResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        /// <summary>Merged cart JSON after login/register. Use this to replace guest cart in frontend.</summary>
        public string Cart { get; set; } = "{}";
    }
}

// M.B