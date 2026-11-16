using System.ComponentModel.DataAnnotations;

namespace toka_fitness.Models
{
    public class User
    {
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Your username must be between 3-30 characters")]
        public required string Username { get; set; }
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Your password must be between 8-50 characters")]
        public required string Password { get; set; }
        [EmailAddress, StringLength(100, ErrorMessage = "Please enter a valid email address")]
        public required string Email { get; set; }

    }
}
