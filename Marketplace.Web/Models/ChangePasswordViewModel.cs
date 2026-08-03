using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Поточний пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новий пароль є обов'язковим")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Пароль має містити щонайменше 8 символів, велику й малу літеру, цифру та спецсимвол")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження пароля є обов'язковим")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
