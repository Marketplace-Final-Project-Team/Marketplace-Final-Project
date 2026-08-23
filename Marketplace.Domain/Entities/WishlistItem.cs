using Marketplace.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Entities
{
    public class WishlistItem : IEntity
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? WishlistId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public Product Product { get; set; }
        public Wishlist Wishlist { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;


        public Guid? WishlistCollectionId { get; set; }
        public WishlistCollection? WishlistCollection { get; set; }
    }
}
