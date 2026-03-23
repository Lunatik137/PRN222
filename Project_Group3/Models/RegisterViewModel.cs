using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter username.")]
    [Display(Name = "Username")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscore.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter email.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select account type.")]
    [Display(Name = "Account Type")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter password.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Phone number must be 10-11 digits.")]
    public string? Phone { get; set; }
}
