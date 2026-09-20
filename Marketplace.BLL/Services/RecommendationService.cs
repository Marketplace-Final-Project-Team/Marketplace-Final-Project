using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
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
                .Select(d => new
                {
                    d.ProductId,
                    d.BusinessId,
                    d.CategoryId,
                    d.Percent,
                    d.DiscountType
                })
                .ToListAsync();

            
            var userDiscounts = await _context.UserDiscounts
                .AsNoTracking()
                .Where(ud =>
                    ud.UserId == userId &&
                    ud.StartDate <= now &&
                    ud.EndDate >= now &&
                    businessIds.Contains(ud.BusinessId))
                .Select(ud => new
                {
                    ud.BusinessId,
                    ud.Percent
                })
                .ToListAsync();

            var productsById = products.ToDictionary(p => p.Id);

            var recommendations = new List<RecommendationDto>();

            foreach (var frequency in productFrequency)
            {
                if (!productsById.TryGetValue(frequency.ProductId, out var product))
                {
                    continue;
                }

                var productDiscount = discounts
                    .Where(d =>
                        d.DiscountType == DiscountType.Product &&
                        d.ProductId == product.Id)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault();

                var categoryDiscount = discounts
                    .Where(d =>
                        d.DiscountType == DiscountType.Category &&
                        d.CategoryId == product.CategoryId &&
                        d.BusinessId == product.BusinessId)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault();

                var businessDiscount = discounts
                    .Where(d =>
                        d.DiscountType == DiscountType.Business &&
                        d.BusinessId == product.BusinessId)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault();

                var userDiscount = userDiscounts
                    .Where(d => d.BusinessId == product.BusinessId)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault();

                decimal bestDiscount = 0;
                DiscountType? bestDiscountType = null;

                
                if (userDiscount != null)
                {
                    var possibleDiscounts = new List<(decimal Percent, DiscountType Type)>();

                    if (productDiscount != null)
                    {
                        possibleDiscounts.Add(
                            (productDiscount.Percent, DiscountType.Product));
                    }

                    if (categoryDiscount != null)
                    {
                        possibleDiscounts.Add(
                            (categoryDiscount.Percent, DiscountType.Category));
                    }

                    if (businessDiscount != null)
                    {
                        possibleDiscounts.Add(
                            (businessDiscount.Percent, DiscountType.Business));
                    }

                    if (userDiscount != null)
                    {
                        possibleDiscounts.Add(
                            (userDiscount.Percent, DiscountType.Product));
                    }

                    var best = possibleDiscounts
                        .OrderByDescending(x => x.Percent)
                        .FirstOrDefault();

                    bestDiscount = best.Percent;
                    bestDiscountType = best.Type;
                }
                else
                {
                    if (productDiscount != null)
                    {
                        bestDiscount = productDiscount.Percent;
                        bestDiscountType = DiscountType.Product;
                    }
                    else if (categoryDiscount != null)
                    {
                        bestDiscount = categoryDiscount.Percent;
                        bestDiscountType = DiscountType.Category;
                    }
                    else if (businessDiscount != null)
                    {
                        bestDiscount = businessDiscount.Percent;
                        bestDiscountType = DiscountType.Business;
                    }
                }

                if (bestDiscount > 100)
                {
                    bestDiscount = 100;
                }

                var finalPrice = bestDiscount > 0
                    ? Math.Round(
                        product.Price * (1m - bestDiscount / 100m),
                        2,
                        MidpointRounding.AwayFromZero)
                    : Math.Round(
                        product.Price,
                        2,
                        MidpointRounding.AwayFromZero);

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