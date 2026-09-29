using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class PostSaleRequest : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
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

        public DateTime? SellerConfirmDeadline { get; set; }
        public string? SellerDisputeReason { get; set; }

        public DateTime? BuyerLastViewedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }
    }
}
