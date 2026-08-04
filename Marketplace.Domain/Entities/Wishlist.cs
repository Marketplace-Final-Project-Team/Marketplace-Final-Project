using Marketplace.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Entities
{
    public class Wishlist : IEntity
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public bool IsDeleted { get; set; } = false;

        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
