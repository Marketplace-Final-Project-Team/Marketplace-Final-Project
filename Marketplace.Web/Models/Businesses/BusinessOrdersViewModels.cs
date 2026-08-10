using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models.Businesses
{
    public class BusinessOrderListItemViewModel
    {
        public Guid OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class BusinessOrdersPageViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public OrderStatus? StatusFilter { get; set; }
        public List<BusinessOrderListItemViewModel> Orders { get; set; } = new();
    }

    public class BusinessOrderLineViewModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public bool BelongsToBusiness { get; set; }
    }

    public class BusinessOrderDetailsViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
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
        public List<BusinessOrderLineViewModel> Items { get; set; } = new();
    }
}
