using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Entities
{
    public class WishlistCollection
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public User User { get; set; } 
        public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
    }
}
