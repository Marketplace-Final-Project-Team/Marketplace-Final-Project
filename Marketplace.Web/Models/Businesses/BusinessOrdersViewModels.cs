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

    public class BusinessTeamMemberViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; }
        public bool IsAccepted { get; set; }
    }

    public class BusinessTeamViewModel
    {
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public bool CanManage { get; set; }
        public List<BusinessTeamMemberViewModel> Members { get; set; } = new();
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
        public List<BusinessPostSaleRequestViewModel> PostSaleRequests { get; set; } = new();
    }

    public class BusinessPostSaleRequestViewModel
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public PostSaleRequestStatus Status { get; set; }
        public string? SellerResponse { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
