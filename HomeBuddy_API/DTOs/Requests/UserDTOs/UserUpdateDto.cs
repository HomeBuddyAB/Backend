namespace HomeBuddy_API.DTOs.Requests.User
{
    public class UserUpdateDto
    {
        public string Email { get; set; } = string.Empty;
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}

//M.B