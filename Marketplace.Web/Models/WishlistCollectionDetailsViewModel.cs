using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class WishlistCollectionDetailsViewModel
    {
        public WishlistCollectionDetailDto Collection { get; set; } = null!;
        public List<WishlistItemDto> AvailableToAdd { get; set; } = new();
    }
}
