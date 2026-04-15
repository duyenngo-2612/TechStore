using System.ComponentModel.DataAnnotations;

namespace TechStore.ViewModel
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        public string OldPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}