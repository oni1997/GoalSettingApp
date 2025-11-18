using System.ComponentModel.DataAnnotations;

namespace GoalSettingApp.Models
{
    public class SignUpModel
    {
        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set;} = "";

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = "";
    }
}