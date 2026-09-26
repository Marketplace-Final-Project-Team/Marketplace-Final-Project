using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class ChatToolsService : IChatToolsService
    {
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Discount> _discountRepo;
        private readonly IGenericRepository<CartItem> _cartItemRepo;
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly IGenericRepository<Category> _categoryRepo;
        private readonly AppDbContext _context;

        public ChatToolsService(
            IGenericRepository<Product> productRepo,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Discount> discountRepo,
            IGenericRepository<CartItem> cartItemRepo,
            IGenericRepository<Cart> cartRepo,
            IHttpContextAccessor httpContextAccessor,
            UserManager<User> userManager,
            IGenericRepository<Category> categoryRepo,
            AppDbContext context
            )
        {
            _productRepo = productRepo;
            _reviewRepo = reviewRepo;
            _discountRepo = discountRepo;
            _cartItemRepo = cartItemRepo;
            _cartRepo = cartRepo;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _categoryRepo = categoryRepo;
            _context = context;
        }

        public async Task<List<ProductGeminiDto>> SearchProductsAsync(string? searchTerm, string? categoryName, string? businessName, float? minRating, decimal? minPrice, decimal? maxPrice, int limit = 10)
        {
            var query = _productRepo.GetQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(normalized) || p.Description.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var normalizedCategory = categoryName.Trim();
                query = query.Where(p => p.Category!.Name == normalizedCategory);
            }

            if (!string.IsNullOrWhiteSpace(businessName))
            {
                var normalizedBusiness = businessName.Trim();
                query = query.Where(p => p.Business!.StoreName.Contains(normalizedBusiness));
            }

            if (minRating.HasValue)
            {
                query = query.Where(p => p.Reviews.Average(r => r.Rating) >= minRating.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            limit = Math.Clamp(limit, 1, 20);

            var products = await query
                .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                .Take(limit)
                .Select(p => new ProductGeminiDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    CategoryName = p.Category!.Name,
                    BusinessName = p.Business!.StoreName,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0
                }).ToListAsync();

            return products;
        }

        public async Task<ProductDetailedGeminiDto?> GetProductDetailsAsync(Guid productId)
        {
            var product = await _productRepo.GetQueryable()
                .Where(p => p.Id == productId)
                .Select(p => new ProductDetailedGeminiDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    OriginalPrice = p.Price,
                    Quantity = p.Quantity,
                    CategoryName = p.Category!.Name,
                    BusinessName = p.Business!.StoreName,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0
                }).FirstOrDefaultAsync();

            if (product == null)
            {
                return null;
            }

            var discount = await _discountRepo.GetQueryable()
                .Where(d => d.ProductId == productId && d.IsActive)
                .FirstOrDefaultAsync();

            if (discount != null)
            {
                product.DiscountPercentage = discount.Percent;
                product.DiscountedPrice = product.OriginalPrice * (1 - discount.Percent / 100);
                product.StartDate = discount.StartDate;
                product.EndDate = discount.EndDate;
            }

            return product;
        }

        public async Task<List<ProductGeminiDto>> CompareProductsAsync(Guid[] productIds)
        {
            if (productIds == null || productIds.Length < 2)
            {
                return new List<ProductGeminiDto>();
            }

            if (productIds.Length > 5)
            {
                productIds = productIds.Take(5).ToArray();
            }

            return await _productRepo.GetQueryable()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new ProductGeminiDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    CategoryName = p.Category!.Name,
                    BusinessName = p.Business!.StoreName,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0
                }).ToListAsync();
        }

        public async Task<List<ReviewGeminiDto>> GetProductReviewsAsync(Guid productId, int limit = 10)
        {
            limit = Math.Clamp(limit, 1, 15);

            return await _reviewRepo.GetQueryable()
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new ReviewGeminiDto
                {
                    Rating = r.Rating,
                    Body = r.Body
                }).ToListAsync();
        }

        public async Task<List<ProductGeminiDto>> GetSimilarProductsAsync(Guid productId, int limit = 5)
        {
            var product = await _productRepo.GetByIdAsync(productId);

            if (product == null)
            {
                return new List<ProductGeminiDto>();
            }

            limit = Math.Clamp(limit, 1, 10);

            var priceMin = product.Price * 0.7m;
            var priceMax = product.Price * 1.3m;

            var similarProducts = await _productRepo
                .GetQueryable()
                .Where(p => p.Id != productId && p.CategoryId == product.CategoryId && p.Price >= priceMin && p.Price <= priceMax)
                .OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0)
                .Take(limit)
                .Select(p => new ProductGeminiDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    CategoryName = p.Category!.Name,
                    BusinessName = p.Business!.StoreName,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0
                })
                .ToListAsync();

            return similarProducts;
        }

        public async Task<AvailabilityGeminiDto> CheckAvailabilityAsync(Guid productId)
        {
            var product = await _productRepo.GetByIdAsync(productId);

            if (product == null)
            {
                return new AvailabilityGeminiDto
                {
                    Exists = false,
                    InStock = false,
                    Quantity = 0
                };
            }

            return new AvailabilityGeminiDto
            {
                Exists = true,
                InStock = product.Quantity > 0,
                Quantity = product.Quantity
            };
        }

        public async Task<List<DiscountGeminiDto>> GetDiscountsAsync(string? categoryName, int limit = 10)
        {
            var query = _discountRepo.GetQueryable().Where(d => d.IsActive);

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var normalized = categoryName.Trim();
                query = query.Where(d => d.Product!.Category!.Name == normalized);
            }

            limit = Math.Clamp(limit, 1, 20);

            return await query
                .OrderByDescending(d => d.Percent)
                .Take(limit)
                .Select(d => new DiscountGeminiDto
                {
                    ProductName = d.Product!.Name,
                    DiscountPercentage = d.Percent,
                    OriginalPrice = d.Product.Price,
                    DiscountedPrice = d.Product.Price * (1 - d.Percent / 100),
                    StartDate = d.StartDate,
                    EndDate = d.EndDate
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> AddToCartAsync(Guid productId, int quantity)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            if (userId == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Потрібна авторизація" };
            }

            if (quantity <= 0)
            {
                return new ServiceResult { Succeeded = false, Message = "Quantity must be a positive number" };
            }

            var product = await _productRepo.GetByIdAsync(productId);

            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Product not found" };
            }

            var cart = await _cartRepo.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await _cartRepo.AddAsync(cart);
            }

            var cartItem = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            var totalQuantity = quantity + (cartItem?.Quantity ?? 0);
            if (product.Quantity < totalQuantity)
            {
                return new ServiceResult { Succeeded = false, Message = "There are not enough items" };
            }

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                await _cartItemRepo.AddAsync(cartItem);
            }
            else
            {
                cartItem.Quantity += quantity;
                _cartItemRepo.Update(cartItem);
            }

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = "Product added to cart successfully" };
        }

        public async Task<ServiceResult> RemoveFromCartAsync(Guid productId)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            if (userId == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Потрібна авторизація" };
            }

            var cart = await _cartRepo.FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Cart is empty" };
            }

            var cartItem = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);

            if (cartItem == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Product not found in cart" };
            }

            _cartItemRepo.Remove(cartItem);

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = "Product removed from cart successfully" };
        }

        public async Task<string[]> GetCategoriesAsync() 
        {
            return await _categoryRepo.GetQueryable().Select(c => c.Name).ToArrayAsync();
        }
    }
}