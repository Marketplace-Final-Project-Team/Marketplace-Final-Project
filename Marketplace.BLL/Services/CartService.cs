using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;

namespace Marketplace.BLL.Services
{
    public class CartService: ICartService
    {
        private readonly IGenericRepository<Cart> _cartRepository;
        private readonly ICartItemRepository _cartItemRepository;  
        private readonly AppDbContext _context;

        public CartService(IGenericRepository<Cart> cartRepository, ICartItemRepository cartItemRepository, AppDbContext context)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _context = context;
        }

        public async Task<List<CartItemDto>> GetCartItems(string userId)
        {
            var cartItems = await _cartItemRepository.GetCartItemsByUserIdAsync(userId);
            return cartItems.Select(ci => new CartItemDto
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ImageUrl = ci.Product.MainImageUrl,
                Price = ci.Product.Price,
                Quantity = ci.Quantity
            }).ToList();
        }

        public async Task<ServiceResult> AddToCart(string userId, Guid productId)
        {
            var cart = await _cartRepository.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) 
            {
                cart = new Cart { UserId = userId };
                await _cartRepository.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId);

            if(item == null)
            {
                item = new CartItem { CartId = cart.Id, ProductId = productId, Quantity = 1 };
                await _cartItemRepository.AddAsync(item);
                await _context.SaveChangesAsync();
            }
            else
            {
                await ChangeQuantity(item, 1);
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty};
        }

        public async Task<ServiceResult> RemoveFromCart(string userId, Guid productId)
        {
            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Item not found in cart." };
            }

            await Remove(item);

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> IncreaseQuantity(string userId, Guid productId)
        {
            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Item not found in cart." };
            }

            await ChangeQuantity(item, 1);

            return new ServiceResult { Succeeded = true, Message = string.Empty};
        }

        public async Task<ServiceResult> DecreaseQuantity(string userId, Guid productId)
        {
            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Item not found in cart." };
            }

            if (item.Quantity <= 1)
            {
                await Remove(item);
            }
            else
            {
                await ChangeQuantity(item, -1);
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty};
        }

        private async Task ChangeQuantity(CartItem item, int temp)
        {
            item.Quantity += temp;
            _cartItemRepository.Update(item);
            await _context.SaveChangesAsync();
        }

        private async Task Remove(CartItem item)
        {
            _cartItemRepository.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
