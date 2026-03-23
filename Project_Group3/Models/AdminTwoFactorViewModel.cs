using System.ComponentModel.DataAnnotations;

namespace Project_Group3.Models;

public class AdminTwoFactorViewModel
{
    [Required(ErrorMessage = "2FA code is required.")]
    [Display(Name = "2FA Code")]
    public string Code { get; set; } = string.Empty;
}
