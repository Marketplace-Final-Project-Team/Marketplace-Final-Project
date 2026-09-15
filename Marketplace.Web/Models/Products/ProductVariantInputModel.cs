using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models.Products
{
    public class ProductVariantInputModel
    {
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        [StringLength(100)]
        public string Sku { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        public int Quantity { get; set; }
    }
}
