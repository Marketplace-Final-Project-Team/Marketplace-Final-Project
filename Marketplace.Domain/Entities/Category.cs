using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class Category: BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public bool IsAdultOnly { get; set; } = false;

        public ICollection<Product> Products { get; set; } = new List<Product>();

        public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }
}
