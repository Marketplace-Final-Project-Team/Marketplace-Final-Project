using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Marketplace.Web.Models.Products
{
    public class ProductEditViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Вкажіть назву товару")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Додайте опис товару")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Ціна має бути більшою за 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        public int Quantity { get; set; }

        [Display(Name = "Артикул (SKU)")]
        [StringLength(100)]
        public string? Sku { get; set; }

        public List<ProductVariantInputModel> Variants { get; set; } = new();

        [Required(ErrorMessage = "Оберіть категорію")]
        public Guid CategoryId { get; set; }

        [Display(Name = "Товар тільки для дорослих (18+)")]
        public bool IsAdultOnly { get; set; }

        public string? ExistingMainImageUrl { get; set; }
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalImages { get; set; }
        public List<string>? ExistingAdditionalImageUrls { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }
}