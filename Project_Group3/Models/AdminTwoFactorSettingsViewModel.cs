using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public sealed class AdminTwoFactorSettingsViewModel
{
    [Display(Name = "Enable 2FA")]
    public bool IsTwoFactorEnabled { get; set; }

    [Display(Name = "Current password")]
    public string? CurrentPassword { get; set; }

    public string? Email { get; set; }

    public string Role { get; set; } = string.Empty;
}
