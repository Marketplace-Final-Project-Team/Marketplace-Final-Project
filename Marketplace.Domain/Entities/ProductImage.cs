using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class ProductImage: BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ImageUrl { get; set; } = string.Empty;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
