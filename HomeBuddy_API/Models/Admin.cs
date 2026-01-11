using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.Models
{
    public class Admin
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string UserName { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        public required string PasswordSalt { get; set; }
    }
}

// M.B