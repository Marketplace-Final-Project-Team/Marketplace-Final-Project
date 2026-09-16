using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs.Businesses
{
    public class BusinessOrderLineDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? VariantSize { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public bool BelongsToBusiness { get; set; }
    }

    public class BusinessOrderDetailsDto
    {
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal OrderTotalAmount { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public string ShippingCountry { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingStreet { get; set; } = string.Empty;
        public List<BusinessOrderLineDto> Items { get; set; } = new();
    }
}
