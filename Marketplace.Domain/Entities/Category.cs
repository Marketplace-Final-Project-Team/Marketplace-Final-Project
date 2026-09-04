using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class Category: BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public bool IsAdultOnly { get; set; } = false;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
