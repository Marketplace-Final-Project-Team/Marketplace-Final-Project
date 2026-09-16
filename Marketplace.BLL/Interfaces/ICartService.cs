using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface ICartService
    {
        Task<ServiceResult> AddToCart(string userId, Guid productId, Guid? variantId = null, int quantity = 1);
        Task<ServiceResult> RemoveFromCart(string userId, Guid productId, Guid? variantId = null);
        Task<ServiceResult> IncreaseQuantity(string userId, Guid productId, Guid? variantId = null);
        Task<ServiceResult> DecreaseQuantity(string userId, Guid productId, Guid? variantId = null);
        Task<List<CartItemDto>> GetCartItems(string userId);
    }
}
