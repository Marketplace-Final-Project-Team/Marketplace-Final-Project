using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models;

public class OrderListItemViewModel
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string? DeliveryMethodLabel { get; set; }
}

public class OrderDetailsViewModel
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingCountry { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingStreet { get; set; } = string.Empty;
    public string? DeliveryMethodLabel { get; set; }
    public string? PaymentMethodLabel { get; set; }
    public List<OrderLineViewModel> Items { get; set; } = new();
}

public class PostSaleRequestViewModel
{
    public string Reason { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public List<string> PhotoUrls { get; set; } = new();
    public DateTime SubmittedAtUtc { get; set; }
    public Marketplace.Domain.Enums.PostSaleRequestStatus Status { get; set; }
    public string? SellerResponse { get; set; }
}

public class OrderLineViewModel
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
    public PostSaleRequestViewModel? ReturnRequest { get; set; }
}
