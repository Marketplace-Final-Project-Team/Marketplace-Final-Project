using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models.Products
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Оберіть бізнес")]
        public Guid BusinessId { get; set; }

        [Required(ErrorMessage = "Вкажіть назву товару")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Додайте опис товару")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна має бути більшою за 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Оберіть категорію")]
        public Guid CategoryId { get; set; }

        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalImages { get; set; }
        public IEnumerable<SelectListItem> Businesses { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }
}