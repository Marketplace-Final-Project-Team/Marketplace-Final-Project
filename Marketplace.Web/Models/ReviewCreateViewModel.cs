using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class ReviewCreateViewModel
    {
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Повідомлення є обов'язковим")]
        [MaxLength(300, ErrorMessage = "Повідомлення не може перевищувати 300 символів")]
        public string Body { get; set; } = string.Empty;

        [Required(ErrorMessage = "Оцінка є обов'язковою")]
        [Range(1, 5, ErrorMessage = "Оцінка повинна бути в діапазоні від 1 до 5")]
        public float Rating { get; set; }
    }
}
