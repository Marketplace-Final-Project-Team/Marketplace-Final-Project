using System.ComponentModel.DataAnnotations;
using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class Order: BaseEntity
    {
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal TotalAmount { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        [MaxLength(100)]
        public string RecipientLastName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string RecipientFirstName { get; set; } = string.Empty;
        [MaxLength(30)]
        public string RecipientPhone { get; set; } = string.Empty;
        public string DeliveryCarrier { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? NovaPoshtaCityRef { get; set; }
        public string? NovaPoshtaBranchRef { get; set; }

        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingStreet { get; set; } = string.Empty;
    }
}
