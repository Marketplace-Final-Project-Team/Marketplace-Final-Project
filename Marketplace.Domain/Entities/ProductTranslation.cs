using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class ProductTranslation : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Culture { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
