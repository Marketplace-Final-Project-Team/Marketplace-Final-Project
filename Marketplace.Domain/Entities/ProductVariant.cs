using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class ProductVariant : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Size { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
