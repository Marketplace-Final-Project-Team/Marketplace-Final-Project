using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class SignInViewModel
    {
        [Required(ErrorMessage = "Емейл є обов'язковим")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
