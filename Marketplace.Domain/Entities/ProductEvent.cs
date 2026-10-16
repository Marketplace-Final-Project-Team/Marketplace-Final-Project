using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class ProductEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }

        public Guid BusinessId { get; set; }

        public ProductEventType Type { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
