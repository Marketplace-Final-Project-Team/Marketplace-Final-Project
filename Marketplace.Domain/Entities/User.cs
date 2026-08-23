using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.Domain.Entities
{
    public class User : IdentityUser, IAuditable
    {
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; } = false;

        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WishlistCollection> WishlistCollections { get; set;} = new List<WishlistCollection>();
    }
}
