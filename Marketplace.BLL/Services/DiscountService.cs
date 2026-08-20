using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _context;

        public DiscountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SetDiscountAsync(Guid productId, int percent, DateTime startDate, DateTime endDate)
        {
            if (percent is < 1 or > 99)
                throw new ArgumentOutOfRangeException(nameof(percent), "Знижка має бути від 1% до 99%.");

            if (endDate <= startDate)
                throw new ArgumentException("Дата закінчення має бути пізніше дати початку.");

       
            var existingDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId)
                .ToListAsync();

            foreach (var old in existingDiscounts)
            {
                old.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }

            var discount = new Discount
            {
                ProductId = productId,
                Percent = percent,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveDiscountAsync(Guid productId)
        {
            var activeDiscounts = await _context.Discounts
                .Where(d => d.ProductId == productId && d.EndDate > DateTime.UtcNow)
                .ToListAsync();

            foreach (var d in activeDiscounts)
            {
                d.EndDate = DateTime.UtcNow.AddSeconds(-1);
            }

            await _context.SaveChangesAsync();
        }
    }
}