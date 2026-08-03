using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class ProductItemAdminViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ProductStatus Status { get; set; }
    }
}
