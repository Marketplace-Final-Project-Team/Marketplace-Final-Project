using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Pricing;
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
                var discountPercent = DiscountCalculator.GetEffectiveDiscountPercent(ci.Product, userId, now);
                var finalPrice = DiscountCalculator.CalculatePrice(ci.Product.Price, discountPercent);

                return new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ImageUrl = ci.Product.MainImageUrl,
                    Price = finalPrice,
                    Quantity = ci.Quantity,
                    StockQuantity = ci.ProductVariant?.Quantity ?? ci.Product.Quantity,
                    ProductVariantId = ci.ProductVariantId,
                    VariantSize = ci.ProductVariant?.Size
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

            var variantResult = ResolveVariant(product, variantId);
            if (!variantResult.Succeeded)
            {
                return variantResult.Error!;
            }
            var variant = variantResult.Variant;
            var availableStock = variant?.Quantity ?? product.Quantity;

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

            var variantResult = ResolveVariant(product, variantId);
            if (!variantResult.Succeeded)
            {
                return variantResult.Error!;
            }
            var availableStock = variantResult.Variant?.Quantity ?? product.Quantity;

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

        private static (bool Succeeded, ProductVariant? Variant, ServiceResult? Error) ResolveVariant(Product product, Guid? variantId)
        {
            if (product.Variants.Count == 0)
            {
                return (true, null, null);
            }

            if (variantId is null)
            {
                return (false, null, new ServiceResult { Succeeded = false, Message = "Оберіть розмір товару." });
            }

            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant is null)
            {
                return (false, null, new ServiceResult { Succeeded = false, Message = "Обраний варіант товару не знайдено." });
            }

            return (true, variant, null);
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
