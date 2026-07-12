using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class Order: BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal TotalAmount { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingStreet { get; set; } = string.Empty;
    }
}
