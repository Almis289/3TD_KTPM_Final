using System.ComponentModel.DataAnnotations;

namespace Book_Store.ViewModels
{
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới (để trống để sinh ngẫu nhiên)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string? ConfirmPassword { get; set; }
    }
}
