using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.Interfaces
{
   public interface IWishlistService
    {
        Task<WishlistDto> GetWishlistAsync(string userId);

        Task<ServiceResult> AddToWishlistAsync(string userId, Guid productId);

        Task<ServiceResult> RemoveFromWishlistAsync(string userId, Guid productId);
        
    }
}
