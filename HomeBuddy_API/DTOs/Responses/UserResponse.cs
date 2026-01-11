namespace HomeBuddy_API.DTOs.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public string Cart { get; set; } = "{}";
    }
}
