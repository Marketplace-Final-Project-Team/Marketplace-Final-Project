using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class Product: BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public ProductStatus Status { get; set; } = ProductStatus.Pending;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
