using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "Введіть ім’я користувача.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Ім’я має містити від 2 до 30 символів.")]
        [RegularExpression(
            @"^[\p{L}\p{N}._-]+$",
            ErrorMessage = "Використовуйте літери, цифри, крапку, дефіс або підкреслення.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Перевірте формат електронної пошти.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Створіть пароль.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Пароль не відповідає вимогам безпеки.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторіть пароль.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Потрібно прийняти правила користування.")]
        public bool AcceptTerms { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
