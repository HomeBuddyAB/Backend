using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models 
{ 
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        public required string PasswordSalt { get; set; }

        [MaxLength(255)]
        public string Cart { get; set; } = "{}";
    }
}

// M.B