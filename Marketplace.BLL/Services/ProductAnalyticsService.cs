using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class ProductAnalyticsService : IProductAnalyticsService
    {
        private readonly AppDbContext _context;

        public ProductAnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RecordEventAsync(Guid productId, Guid businessId, ProductEventType type, string? userId)
        {
            _context.ProductEvents.Add(new ProductEvent
            {
                ProductId = productId,
                BusinessId = businessId,
                Type = type,
                UserId = userId
            });

            await _context.SaveChangesAsync();
        }

        public async Task<BusinessTrafficStatsDto> GetBusinessTrafficStatsAsync(Guid businessId, DateTime startDate, DateTime endDate)
        {
            var events = await _context.ProductEvents
                .AsNoTracking()
                .Where(e => e.BusinessId == businessId
                    && e.CreatedAt.Date >= startDate.Date
                    && e.CreatedAt.Date < endDate.AddDays(1).Date)
                .Select(e => new { e.ProductId, e.Type })
                .ToListAsync();

            var byProduct = events
                .GroupBy(e => e.ProductId)
                .Select(g => new ProductTrafficStatsDto
                {
                    ProductId = g.Key,
                    Views = g.Count(e => e.Type == ProductEventType.View),
                    CartAdds = g.Count(e => e.Type == ProductEventType.AddToCart),
                    WishlistAdds = g.Count(e => e.Type == ProductEventType.AddToWishlist)
                })
                .ToList();

            return new BusinessTrafficStatsDto
            {
                TotalViews = events.Count(e => e.Type == ProductEventType.View),
                TotalCartAdds = events.Count(e => e.Type == ProductEventType.AddToCart),
                TotalWishlistAdds = events.Count(e => e.Type == ProductEventType.AddToWishlist),
                ByProduct = byProduct
            };
        }
    }
}
