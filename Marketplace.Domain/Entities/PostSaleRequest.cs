using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class PostSaleRequest : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        // Returns are scoped to a single line item, not the whole order — a multi-seller
        // order can have one item returned without touching the other sellers' money/stock.
        public Guid OrderItemId { get; set; }
        public OrderItem OrderItem { get; set; } = null!;

        public string BuyerId { get; set; } = string.Empty;
        public User Buyer { get; set; } = null!;

        public string Reason { get; set; } = string.Empty;
        public string? Comment { get; set; }

        public ICollection<PostSaleRequestImage> Images { get; set; } = new List<PostSaleRequestImage>();

        public PostSaleRequestStatus Status { get; set; } = PostSaleRequestStatus.Pending;
        public string? SellerResponse { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
