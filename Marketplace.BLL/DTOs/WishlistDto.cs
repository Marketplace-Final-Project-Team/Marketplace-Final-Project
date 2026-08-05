using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class WishlistDto
    {
        public Guid Id { get; set; }
        public List<WishlistItemDto> Items { get; set; } = new();
        public int TotalItems => Items?.Count ?? 0;

    }
}
