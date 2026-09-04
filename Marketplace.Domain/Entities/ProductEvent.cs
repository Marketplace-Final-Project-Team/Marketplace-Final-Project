using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    // Deliberately not : BaseEntity — an append-only analytics trail, so it
    // has no IsDeleted/UpdatedAt and no soft-delete query filter.
    public class ProductEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }

        // Denormalized so per-business queries don't need to join Product.
        public Guid BusinessId { get; set; }

        public ProductEventType Type { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
