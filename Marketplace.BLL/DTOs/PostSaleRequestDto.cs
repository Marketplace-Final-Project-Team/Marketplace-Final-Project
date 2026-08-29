using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class PostSaleRequestDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string BuyerId { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public PostSaleRequestStatus Status { get; set; }
        public string? SellerResponse { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
