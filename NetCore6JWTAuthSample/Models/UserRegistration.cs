using System.ComponentModel.DataAnnotations;

namespace NetCore6JWTAuthSample.Models
{
    public class UserRegistration
    {
        [Required(ErrorMessage = "User Name is invalid.")]
        public string? Username { get; set; }

        [EmailAddress]
        [Required(ErrorMessage = "Email is invalid.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is invalid.")]
        public string? Password { get; set; }
    }
}
