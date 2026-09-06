using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.AspNetCore.Identity;
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
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Business)
                        .ThenInclude(b => b.Discounts)
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                        .ThenInclude(c => c.Discounts)
                

                .ToListAsync();
        }
    }
}