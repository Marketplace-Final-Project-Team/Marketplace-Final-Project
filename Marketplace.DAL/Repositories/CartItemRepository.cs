using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL.Repositories
{
    public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
    {
        public CartItemRepository(AppDbContext context) : base(context) { }

        public async Task<List<CartItem>> GetCartItemsByUserIdAsync(string userId)
        {
            return await _context.CartItems
                .Where(ci => ci.Cart.UserId == userId)
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Discounts)
                .ToListAsync();
        }
    }
}