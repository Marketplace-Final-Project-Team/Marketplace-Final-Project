using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class Review: BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }
    }
}
