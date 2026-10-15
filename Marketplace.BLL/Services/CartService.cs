using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;

namespace Marketplace.BLL.Services
{
    public class CartService : ICartService
    {
        private readonly IGenericRepository<Cart> _cartRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly AppDbContext _context;
        private readonly IProductAnalyticsService _productAnalyticsService;

        public CartService(
            IGenericRepository<Cart> cartRepository,
            ICartItemRepository cartItemRepository,
            IGenericRepository<Product> productRepository,
            AppDbContext context,
            IProductAnalyticsService productAnalyticsService)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _context = context;
            _productAnalyticsService = productAnalyticsService;
        }

        public async Task<List<CartItemDto>> GetCartItems(string userId)
        {
            var cartItems = await _cartItemRepository.GetCartItemsByUserIdAsync(userId);
            var now = DateTime.UtcNow;

            return cartItems.Select(ci =>
            {
                var productDiscount = ci.Product.Discounts?
                .FirstOrDefault(d => d.StartDate <= now && d.EndDate >= now && d.DiscountType == DiscountType.Product);


                var businessDiscount = ci.Product.Business?.Discounts?
                    .FirstOrDefault(d => d.BusinessId == ci.Product.BusinessId && d.DiscountType == DiscountType.Business && d.StartDate <= now && d.EndDate >= now);


                var categoryDiscount = ci.Product.Category?.Discounts?
                    .FirstOrDefault(d => d.BusinessId == ci.Product.BusinessId && d.CategoryId == ci.Product.CategoryId && d.DiscountType == DiscountType.Category && d.StartDate <= now && d.EndDate >= now);
                var userDiscount = _context.UserDiscounts
                    .FirstOrDefault(ud => ud.UserId == userId && ud.BusinessId == ci.Product.BusinessId && ud.StartDate <= now && ud.EndDate >= now);

                decimal discountPercent = 0;

                if (userDiscount != null)
                {
                    var alldiscounts = new List<decimal>();
                    if (productDiscount != null)
                    {
                        alldiscounts.Add(productDiscount.Percent);
                    }
                    if (categoryDiscount != null)
                    {
                        alldiscounts.Add(categoryDiscount.Percent);
                    }
                    if (userDiscount != null)
                    {
                        alldiscounts.Add(userDiscount.Percent);
                    }
                    if (businessDiscount != null)
                    {
                        alldiscounts.Add(businessDiscount.Percent);
                    }
                    discountPercent = alldiscounts.Any() ? alldiscounts.Max() : 0;
                }
                else
                {
                    if (productDiscount != null)
                    {
                        discountPercent = productDiscount.Percent;
                    }
                    else if (categoryDiscount != null)
                    {
                        discountPercent = categoryDiscount.Percent;

                    }

                    else if (businessDiscount != null)
                    {
                        discountPercent = businessDiscount.Percent;
                    }
                }

                decimal finalPrice = discountPercent > 0
                    ? ci.Product.Price * (1 - discountPercent / 100m)
                    : ci.Product.Price;

                return new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ImageUrl = ci.Product.MainImageUrl,
                    Price = finalPrice,
                    Quantity = ci.Quantity,
                    StockQuantity = ci.Product.Quantity,
                    ProductVariantId = ci.ProductVariantId,
                };
            }).ToList();
        }

        public async Task<ServiceResult> AddToCart(string userId, Guid productId, Guid? variantId = null, int quantity = 1)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            var product = await _productRepository.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Status != ProductStatus.Active)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
            }

            var availableStock = product.Quantity;

            if (availableStock <= 0)
            {
                return new ServiceResult { Succeeded = false, Message = "Товару немає в наявності." };
            }

            var cart = await _cartRepository.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepository.AddAsync(cart);
                await _context.SaveChangesAsync();
            }

            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId && ci.ProductVariantId == variantId);

            if (item == null)
            {
                item = new CartItem { CartId = cart.Id, ProductId = productId, ProductVariantId = variantId, Quantity = Math.Min(quantity, availableStock) };
                await _cartItemRepository.AddAsync(item);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (item.Quantity >= availableStock)
                {
                    return new ServiceResult
                    {
                        Succeeded = false,
                        Message = "У кошику вже максимальна доступна кількість товару."
                    };
                }

                await ChangeQuantity(item, Math.Min(quantity, availableStock - item.Quantity));
            }

            await _productAnalyticsService.RecordEventAsync(productId, product.BusinessId, ProductEventType.AddToCart, userId);

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> RemoveFromCart(string userId, Guid productId, Guid? variantId = null)
        {
            var product = await _productRepository.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
            }

            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId && ci.ProductVariantId == variantId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено у кошику." };
            }

            await Remove(item);

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> IncreaseQuantity(string userId, Guid productId, Guid? variantId = null)
        {
            var product = await _productRepository.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Status != ProductStatus.Active)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
            }

            var availableStock = product.Quantity;

            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId && ci.ProductVariantId == variantId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено у кошику." };
            }

            if (item.Quantity >= availableStock)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Більше одиниць цього товару немає на складі."
                };
            }

            await ChangeQuantity(item, 1);

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> DecreaseQuantity(string userId, Guid productId, Guid? variantId = null)
        {
            var product = await _productRepository.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
            }

            var item = await _cartItemRepository.FirstOrDefaultAsync(ci => ci.Cart.UserId == userId && ci.ProductId == productId && ci.ProductVariantId == variantId);

            if (item == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар не знайдено у кошику." };
            }

            if (item.Quantity <= 1)
            {
                await Remove(item);
            }
            else
            {
                await ChangeQuantity(item, -1);
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty };
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
