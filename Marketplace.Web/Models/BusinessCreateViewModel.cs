using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class BusinessCreateViewModel
    {
        [Required(ErrorMessage = "Назва магазину є обов'язковою")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Назва магазину має бути від 2 до 100 символів")]
        public string StoreName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Опис магазину є обов'язковим")]
        [StringLength(2000, ErrorMessage = "Опис не повинен перевищувати 2000 символів")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Логотип є обов'язковим")]
        public IFormFile Logo { get; set; } = null!;

        [Required(ErrorMessage = "Банер є обов'язковим")]
        public IFormFile Banner { get; set; } = null!;
    }
}
