using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class CartItem : BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }
    }
}