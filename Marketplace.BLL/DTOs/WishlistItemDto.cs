using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class WishlistItemDto
    {
        
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        public string MainImageUrl { get; set; } = string.Empty;
        





    }
}
