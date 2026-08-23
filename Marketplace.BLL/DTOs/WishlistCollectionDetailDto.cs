using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class WishlistCollectionDetailDto : WishlistCollectionDto
    {
        public List<WishlistItemDto> Items { get; set; } = new();
    }
}
