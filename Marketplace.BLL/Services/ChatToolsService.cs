using DocumentFormat.OpenXml.Math;
using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.DTOs;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.Metadata.Ecma335;

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
        private readonly IGenericRepository<BusinessMember> _businessMemberRepo;
        private readonly IGenericRepository<Business> _businessRepo;
        private readonly OrderRepository _orderRepo;
        private readonly BusinessAccessService _businessAccessService;
        private readonly AppDbContext _context;

        private readonly DateTime RightNow = DateTime.UtcNow.Date;

        public ChatToolsService(
            IGenericRepository<Product> productRepo,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Discount> discountRepo,
            IGenericRepository<CartItem> cartItemRepo,
            IGenericRepository<Cart> cartRepo,
            IHttpContextAccessor httpContextAccessor,
            UserManager<User> userManager,
            IGenericRepository<Category> categoryRepo,
            IGenericRepository<BusinessMember> businessMemberRepo,
            IGenericRepository<Business> businessRepo,
            OrderRepository orderRepo,
            BusinessAccessService buisnessAccessService,
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
            _businessMemberRepo = businessMemberRepo;
            _businessRepo = businessRepo;
            _orderRepo = orderRepo;
            _businessAccessService = buisnessAccessService;
            _context = context;
        }


        //Customer
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

        //Business
        public async Task<BusinessSummaryDto[]> GetMyBusinessesAsync()
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            if (userId == null)
            {
                return [];
            }

            var ownerBusinesses = await _businessRepo
                .GetQueryable()
                .Where(b => b.OwnerId == userId)
                .Select(b => new BusinessSummaryDto
                {
                    Id = b.Id,
                    StoreName = b.StoreName
                })
                .ToArrayAsync();

            var memberBusinesses = await _businessMemberRepo
                .GetQueryable()
                .Where(bm => bm.UserId == userId && bm.IsAccepted)
                .Select(bm => new BusinessSummaryDto
                {
                    Id = bm.Business.Id,
                    StoreName = bm.Business.StoreName
                })
                .ToArrayAsync();

            var businesses = ownerBusinesses
                .Concat(memberBusinesses)
                .GroupBy(b => b.Id)
                .Select(g => g.First())
                .ToArray();

            return businesses;
        }

        public async Task<DataResult<SalesSummaryDto>> GetSalesSummaryAsync(Guid businessId, DateTime? startDate, DateTime? endDate)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });
            if (!accessResult.Succeeded)
            {
                return new DataResult<SalesSummaryDto> { Succeeded = false, Message = accessResult.Message };
            }

            var periodResult = ResolvePeriod(startDate, endDate);
            if (!periodResult.Succeeded)
            {
                return new DataResult<SalesSummaryDto> { Succeeded = false, Message = periodResult.Message };
            }
            var period = periodResult.Data!;

            var stat = await _orderRepo.GetBusinessStatByPeriodAsync(businessId, period.StartDate, period.EndDate);


            var totalRevenue = stat.Sum(s => s.Revenue);
            var orderCount = stat.Sum(s => s.OrderCount);
            var aov = orderCount > 0 ? totalRevenue / orderCount : 0;

            return new DataResult<SalesSummaryDto>
            {
                Succeeded = true,
                Message = "Sales summary retrieved successfully",
                Data = new SalesSummaryDto
                {
                    TotalRevenue = totalRevenue,
                    OrderCount = orderCount,
                    AOV = aov
                }
            };
        }

        public async Task<DataResult<ProductStatsByPeriodDto[]>> GetProductsSalesStatsAsync(Guid businessId, DateTime? startDate, DateTime? endDate, string sortBy, int limit = 5)
        {
            limit = Math.Clamp(limit, 1, 20);

            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });
            if (!accessResult.Succeeded)
            {
                return new DataResult<ProductStatsByPeriodDto[]> { Succeeded = false, Message = accessResult.Message };
            }

            var periodResult = ResolvePeriod(startDate, endDate);
            if (!periodResult.Succeeded)
            {
                return new DataResult<ProductStatsByPeriodDto[]> { Succeeded = false, Message = periodResult.Message };
            }
            var period = periodResult.Data!;

            var stat = (await _orderRepo.GetProductsStatsByPeriodAsync(businessId, period.StartDate, period.EndDate)).AsQueryable();

            if (sortBy == "best")
            {
                stat = stat.OrderByDescending(s => s.TotalSales);
            }
            else if (sortBy == "worst")
            {
                stat = stat.OrderBy(s => s.TotalSales);
            }

            var res = stat.Take(limit).ToArray();

            return new DataResult<ProductStatsByPeriodDto[]> { Succeeded = true, Message = "Product sales stats retrieved successfully", Data = res };
        }

        public async Task<DataResult<decimal>> GetBusinessBalanceAsync(Guid businessId)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });
            if (!accessResult.Succeeded)
            {
                return new DataResult<decimal> { Succeeded = false, Message = accessResult.Message };
            }

            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null)
            {
                return new DataResult<decimal> { Succeeded = false, Message = "Business not found" };
            }

            return new DataResult<decimal> { Succeeded = true, Data = business.Balance };
        }

        public async Task<DataResult<ReviewSummaryDto[]>> GetRecentReviewsAsync(Guid businessId, int limit = 5)
        {
            limit = Math.Clamp(limit, 1, 20);

            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });

            if (!accessResult.Succeeded)
            {
                return new DataResult<ReviewSummaryDto[]> { Succeeded = false, Message = accessResult.Message };
            }

            var res = await _reviewRepo.GetQueryable()
                .Where(r => r.Product.BusinessId == businessId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new ReviewSummaryDto
                {
                    ProductName = r.Product.Name,
                    Rating = r.Rating,
                    Body = r.Body,
                    CreatedAt = r.CreatedAt
                })
                .ToArrayAsync();

            return new DataResult<ReviewSummaryDto[]> { Succeeded = true, Message = string.Empty, Data = res };
        }

        public async Task<DataResult<ProductGeminiDto[]>> GetOutOfStockProductsAsync(Guid businessId)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.ProductManager });
            if (!accessResult.Succeeded)
            {
                return new DataResult<ProductGeminiDto[]> { Succeeded = false, Message = accessResult.Message };
            }

            var res = await _productRepo.GetQueryable()
                .Where(p => p.BusinessId == businessId && p.Quantity <= 0)
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
                .ToArrayAsync();

            return new DataResult<ProductGeminiDto[]> { Succeeded = true, Message = string.Empty, Data = res };
        }

        public async Task<DataResult<ProductGeminiDto[]>> GetLowStockProductsAsync(Guid businessId, int threshold = 5)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.ProductManager });
            if (!accessResult.Succeeded)
            {
                return new DataResult<ProductGeminiDto[]> { Succeeded = false, Message = accessResult.Message };
            }

            var res = await _productRepo.GetQueryable()
                .Where(p => p.BusinessId == businessId && p.Quantity > 0 && p.Quantity <= threshold)
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
                .ToArrayAsync();
            return new DataResult<ProductGeminiDto[]> { Succeeded = true, Message = string.Empty, Data = res };
        }

        public async Task<DataResult<float>> GetAverageRatingForProductAsync(Guid businessId, Guid productId)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });
            if (!accessResult.Succeeded)
            {
                return new DataResult<float> { Succeeded = false, Message = "Access denied", Data = 0f };
            }

            var reviews = await _reviewRepo
                .GetQueryable()
                .Where(r => r.ProductId == productId && r.Product.BusinessId == businessId)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new DataResult<float> { Succeeded = true, Message = "There are no reviews for this product", Data = 0f };
            }
            var averageRating = reviews.Average(r => r.Rating);

            return new DataResult<float> { Succeeded = true, Message = string.Empty, Data = averageRating };
        }

        public async Task<DataResult<object>> GetReviewsForProductAsync(Guid businessId, Guid productId, int limit = 5)
        {
            limit = Math.Clamp(limit, 1, 20);

            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin, BusinessMemberRole.BusinessAnalyst });
            if (!accessResult.Succeeded)
                return new DataResult<object> { Succeeded = false, Message = "Access denied", Data = null };

            var productExists = await _productRepo.GetQueryable()
                .AnyAsync(p => p.Id == productId && p.BusinessId == businessId);

            if (!productExists)
                return new DataResult<object> { Succeeded = false, Message = "Product not found", Data = null };

            var reviews = await _reviewRepo.GetQueryable()
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(limit)
                .Select(r => new { r.Id, r.UserId, r.User.UserName, r.Rating })
                .ToArrayAsync();

            return new DataResult<object> { Succeeded = true, Message = string.Empty, Data = reviews };
        }

        public async Task<DataResult<object>> GetBusinessMembersAsync(Guid businessId)
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            var userId = httpUser != null ? _userManager.GetUserId(httpUser) : null;

            var accessResult = await CheckUserAccessAsync(businessId, userId, new[] { BusinessMemberRole.Owner, BusinessMemberRole.Admin });
            if (!accessResult.Succeeded)
            {
                return new DataResult<object> { Succeeded = false, Message = "Access denied", Data = null };
            }

            var members = await _businessMemberRepo.GetQueryable()

                .Where(m => m.BusinessId == businessId)
                .Select(m => new { m.UserId, m.User.UserName, m.Role })
                .ToListAsync();

            return new DataResult<object> { Succeeded = true, Message = string.Empty, Data = members };
        }

        public Task<string> GetWithdrawalInstructionsAsync()
        {
            const string instructions =
                """
                Щоб подати заявку на виведення коштів з балансу магазину:
                
                1. Ця дія доступна лише власнику магазину (Owner) — адміністратори та інші
                   учасники команди подавати заявки не можуть.
                2. Перейдіть у Панель продавця → розділ виведення коштів.
                3. Вкажіть суму виведення, номер картки та ім'я отримувача.
                4. Сума заявки не може перевищувати доступний баланс магазину за вирахуванням
                   усіх вже поданих, але ще не оброблених заявок.
                5. Після подання заявка отримує статус "Очікує обробки" (Pending) і передається
                   на розгляд адміністрації маркетплейсу.
                6. Коли адміністратор підтвердить заявку, кошти буде виплачено на вказану картку,
                   а баланс магазину зменшиться на суму виплати.
                7. Якщо заявку відхилено — побачите причину відхилення в історії заявок.
        
                Можна переглянути статус і історію всіх своїх заявок у відповідному розділі
                Панелі продавця.
                """;

            return Task.FromResult(instructions);
        }

        public Task<string> GetModerationPolicySummaryAsync()
        {
            const string policy =
                """
                Правила модерації товарів на маркетплейсі:

                Перед публікацією кожен товар (текст і зображення) автоматично перевіряється
                на наявність забороненого контенту, зокрема:
                - Контент 18+ (порнографія, оголене тіло)
                - Насильство, зброя, шокуючий чи жорстокий контент
                - Наркотики та заборонені речовини
                - Ненормативна лексика, образи, погрози
                - Ознаки шахрайства чи введення покупців в оману (підробки, фейкові бренди)

                Якщо хоча б один елемент товару (текст або будь-яке зображення) порушує ці
                правила — товар не проходить модерацію і не публікується в каталозі.

                Причину відхилення можна побачити в розділі "Мої товари" поруч із товаром.
                Після виправлення контенту товар можна надіслати на повторну перевірку.
                """;

            return Task.FromResult(policy);
        }

        public Task<string> GetDiscountSetupInstructionsAsync()
        {
            const string instructions =
                """
                На маркетплейсі можна встановити знижку трьома способами:

                1. Знижка на конкретний товар:
                   - Відкрийте сторінку товару в розділі "Мої товари" та встановіть відсоток
                     знижки, дату початку та дату закінчення.
                   - Якщо на товар вже діє знижка, вона автоматично скасовується і замінюється новою.

                2. Знижка на весь бізнес (усі товари магазину одразу):
                   - Доступна в Панелі продавця, розділ знижок бізнесу.
                   - Одночасно може діяти лише одна знижка на весь бізнес — нова автоматично
                     скасовує попередню.
        
                3. Знижка на категорію (усі товари певної категорії в межах вашого магазину):
                   - Вкажіть категорію, відсоток знижки, дату початку та закінчення.
                   - Не можна створити нову знижку на категорію, поки діє активна знижка для
                     цієї ж категорії — попередню потрібно спочатку скасувати.
        
                Загальні обмеження для всіх типів знижок:
                - Відсоток знижки має бути в межах від 1% до максимально дозволеного
                  (типово 90%, точне значення може бути змінене адміністрацією).
                - Дата початку не може бути в минулому.
                - Дата закінчення обов'язково пізніша за дату початку.

                Встановлювати та скасовувати знижки можуть власник магазину та учасники
                команди з відповідними правами доступу (керування товарами).

                Скасувати знижку можна в тому ж розділі, де вона була встановлена —
                достроково завершивши її дію.
                """;

            return Task.FromResult(instructions);
        }

        //Допоміжні методи
        private async Task<ServiceResult> CheckUserAccessAsync(Guid businessId, string? userId, BusinessMemberRole[] roles)
        {
            if (userId == null)
                return new ServiceResult { Succeeded = false, Message = "User not found" };

            var hasAccess = await _businessAccessService.HasAccessAsync(businessId, userId, roles);

            if (!hasAccess)
                return new ServiceResult { Succeeded = false, Message = "Access denied" };

            return new ServiceResult { Succeeded = true, Message = "" };
        }


        private static DataResult<PeriodDto> ResolvePeriod(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate > endDate)
                    return new DataResult<PeriodDto> { Succeeded = false, Message = "Invalid date range" };

                return new DataResult<PeriodDto>
                {
                    Succeeded = true,
                    Data = new PeriodDto { StartDate = startDate.Value, EndDate = endDate.Value }
                };
            }

            return new DataResult<PeriodDto>
            {
                Succeeded = true,
                Data = new PeriodDto { StartDate = DateTime.UtcNow.AddDays(-30), EndDate = DateTime.UtcNow }
            };
        }

        public Task<string> GetBulkImportInstructionsAsync()
        {
            const string instructions =
                """
                Щоб масово додати товари на склад через таблицю:
        
                1. Перейдіть у Панель продавця → розділ "Товари" → "Масовий імпорт".
                2. Завантажте шаблон Excel-файлу (.xlsx) за посиланням на сторінці імпорту.
                3. Заповніть таблицю — кожен рядок це окремий товар. Обов'язкові колонки:
                   назва, опис, ціна, кількість, категорія.
                4. Збережіть файл і завантажте його через кнопку "Завантажити файл" на сторінці імпорту.
                5. Система перевірить файл і покаже попередній перегляд — перевірте дані перед підтвердженням.
                6. Натисніть "Підтвердити імпорт" — товари з'являться у вашому каталозі.
        
                Якщо є помилки у файлі (неправильний формат ціни, відсутня категорія тощо),
                система покаже список рядків з помилками, які потрібно виправити.
                """;

            return Task.FromResult(instructions);
        }
    }
}