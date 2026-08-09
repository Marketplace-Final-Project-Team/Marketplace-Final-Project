using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    // Not a persisted notification (no such table exists) — derived read-only from each
    // order's current status and BaseEntity.UpdatedAt/CreatedAt timestamp. There is no
    // read/unread state and no history of intermediate status changes.
    public class ActivityItemViewModel
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
