using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class Cart : BaseEntity
    {
         public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}