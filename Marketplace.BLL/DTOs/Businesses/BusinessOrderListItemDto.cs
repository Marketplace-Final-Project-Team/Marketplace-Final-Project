using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs.Businesses
{
    public class BusinessOrderListItemDto
    {
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingStreet { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
