using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Entities
{
    public class WishlistItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid WishlistId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public Product Product { get; set; }
        public Wishlist Wishlist { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
