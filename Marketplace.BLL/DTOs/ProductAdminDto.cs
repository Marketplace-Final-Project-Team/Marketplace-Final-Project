using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class ProductAdminDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Business { get; set; } = string.Empty;
        public ProductStatus Status { get; set; }

    }
}
