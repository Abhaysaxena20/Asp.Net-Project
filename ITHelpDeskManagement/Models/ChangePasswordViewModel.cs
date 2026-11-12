
using System.ComponentModel.DataAnnotations;

namespace ITHelpDeskManagement.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword",
            ErrorMessage = "New password and confirm password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
