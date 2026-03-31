using System.ComponentModel.DataAnnotations;

namespace JSAPNEW.Models
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string NewPassword { get; set; }
        [Required]
        public int userId { get; set; }
        [Required]
        public int updatedBy { get; set; }
    }

    public class ChangePasswordRequest2
    {
        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string NewPassword { get; set; }
        [Required]
        public int userId { get; set; }
        [Required]
        public int updatedBy { get; set; }
    }
    public class ChangePasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
