using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Pricing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;

        public RecommendationService(
            AppDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<RecommendationDto>> GetRecomendationAsync(
            string userId,
            int count = 5)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new List<RecommendationDto>();
            }

            var now = DateTime.UtcNow;

            
            var purchasedProductIds = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order.UserId == userId)
                .Select(oi => oi.ProductId)
                .Distinct()
                .ToListAsync();

            if (!purchasedProductIds.Any())
            {
                return new List<RecommendationDto>();
            }

            
            var productFrequency = await _context.OrderItems
                .AsNoTracking()
                .Where(oi =>
                    oi.Order.UserId != userId &&
                    !purchasedProductIds.Contains(oi.ProductId) &&
                    _context.OrderItems.Any(other =>
                        other.OrderId == oi.OrderId &&
                        purchasedProductIds.Contains(other.ProductId)))
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToListAsync();

            if (!productFrequency.Any())
            {
                return new List<RecommendationDto>();
            }

            var productIds = productFrequency
                .Select(x => x.ProductId)
                .ToList();

           
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.MainImageUrl,
                    p.Price,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    p.BusinessId,
                    StoreName = p.Business.StoreName,
                    p.Quantity,
                    p.IsAdultOnly
                })
                .ToListAsync();

            if (!products.Any())
            {
                return new List<RecommendationDto>();
            }

            var businessIds = products
                .Select(p => p.BusinessId)
                .Distinct()
                .ToList();

            var categoryIds = products
                .Select(p => p.CategoryId)
                .Distinct()
                .ToList();

            
            var discounts = await _context.Discounts
                .AsNoTracking()
                .Where(d =>
                    d.EndDate >= now &&
                    d.StartDate <= now &&
                    (
                        (d.DiscountType == DiscountType.Product &&
                         d.ProductId.HasValue &&
                         productIds.Contains(d.ProductId.Value))

                        ||

                        (d.DiscountType == DiscountType.Business &&
                         d.BusinessId.HasValue &&
                         businessIds.Contains(d.BusinessId.Value))

                        ||

                        (d.DiscountType == DiscountType.Category &&
                         d.CategoryId.HasValue &&
                         categoryIds.Contains(d.CategoryId.Value))
                    ))
                .ToListAsync();

            
            var userDiscounts = await _context.UserDiscounts
                .AsNoTracking()
                .Where(ud =>
                    ud.UserId == userId &&
                    ud.StartDate <= now &&
                    ud.EndDate >= now &&
                    businessIds.Contains(ud.BusinessId))
                .ToListAsync();

            var productsById = products.ToDictionary(p => p.Id);

            var recommendations = new List<RecommendationDto>();

            foreach (var frequency in productFrequency)
            {
                if (!productsById.TryGetValue(frequency.ProductId, out var product))
                {
                    continue;
                }

                var bestDiscount = DiscountCalculator.GetEffectiveDiscountPercent(
                    product.Id, product.BusinessId, product.CategoryId, discounts, userDiscounts, userId, now);
                var finalPrice = DiscountCalculator.CalculatePrice(product.Price, bestDiscount);

                recommendations.Add(new RecommendationDto
                {
                    ProductId = product.Id,

                    ProductName = product.Name,
                    ProductDescription = product.Description,
                    ProductImageUrl = product.MainImageUrl,

                    Price = product.Price,

                    DiscountPercent = bestDiscount,
                    FinalPrice = finalPrice,
                    

                    CategoryId = product.CategoryId,
                    CategoryName = product.CategoryName,

                    BusinessId = product.BusinessId,
                    StoreName = product.StoreName,

                    Quantity = product.Quantity,
                    IsAdultOnly = product.IsAdultOnly,

                    Score = frequency.Count
                });
            }

            return recommendations;
        }
    }
}