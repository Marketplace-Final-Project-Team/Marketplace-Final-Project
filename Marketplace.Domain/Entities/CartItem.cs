using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid? ProductVariantId { get; set; }

        public int Quantity { get; set; }
    }
}