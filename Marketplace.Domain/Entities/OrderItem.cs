using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class OrderItem: BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        // Set once, when the order first becomes Confirmed, from the marketplace commission
        // rate at that moment — kept as a snapshot (not recomputed) so a later change to the
        // commission rate can't corrupt the reversal if this order is later canceled.
        public decimal CommissionAmount { get; set; }

        // Guards against refunding the same line item twice (e.g. a return approved after
        // the whole order was separately canceled).
        public bool IsReturned { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
