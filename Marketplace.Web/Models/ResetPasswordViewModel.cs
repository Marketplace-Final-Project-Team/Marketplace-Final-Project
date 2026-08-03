using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новий пароль обов'язковий")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Пароль має містити щонайменше 8 символів, велику й малу літеру, цифру та спецсимвол")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження паролю обов'язкове")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Паролі не співпадають")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
