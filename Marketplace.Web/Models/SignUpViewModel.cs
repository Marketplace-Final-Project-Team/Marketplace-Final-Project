using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ім'я має бути від 2 до 30 символів")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Емейл є обов'язковим")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Пароль має містити щонайменше 8 символів, велику й малу літеру, цифру та спецсимвол")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердження пароля є обов'язковим")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Паролі не співпадають")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
