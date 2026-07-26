using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;

namespace Marketplace.DAL.Interfaces
{
    public interface ICartItemRepository: IGenericRepository<CartItem>
    {
        Task<List<CartItem>> GetCartItemsByUserIdAsync(string userId);
    }
}
