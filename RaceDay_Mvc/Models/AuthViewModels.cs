using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public record ApiUser(int UserId, string FirstName, string LastName, string Email, string Role);

    public class LoginViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, StringLength(50), Display(Name = "First name")] public string FirstName { get; set; } = "";
        [Required, StringLength(50), Display(Name = "Last name")] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters."), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match."), DataType(DataType.Password), Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = "";
        [Required, RegularExpression("Organiser|Participant", ErrorMessage = "Choose a role.")] public string Role { get; set; } = "Participant";
        [Phone, Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
        [DataType(DataType.Date), Display(Name = "Date of birth")] public DateTime? DateOfBirth { get; set; }
    }
}
