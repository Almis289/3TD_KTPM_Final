using System.ComponentModel.DataAnnotations;

namespace Book_Store.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu ít nhất 6 ký tự.")]
        public string Password { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = "";

        public string? Phone { get; set; }
        public string? Address { get; set; }
    }
}
