using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class SignInViewModel
    {
        [Required(ErrorMessage = "Введіть електронну пошту.")]
        [EmailAddress(ErrorMessage = "Перевірте формат електронної пошти.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Залишатися в системі")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
