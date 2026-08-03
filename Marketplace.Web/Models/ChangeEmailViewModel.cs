using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class ChangeEmailViewModel
    {
        [Required(ErrorMessage = "Новий емейл є обов'язковим")]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поточний пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
