using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface ICartService
    {
        Task<ServiceResult> AddToCart(string userId, Guid productId);
        Task<ServiceResult> RemoveFromCart(string userId, Guid productId);
        Task<ServiceResult> IncreaseQuantity(string userId, Guid productId);
        Task<ServiceResult> DecreaseQuantity(string userId, Guid productId);
        Task<List<CartItemDto>> GetCartItems(string userId);
    }
}
