using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class Category: BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
