using Marketplace.DAL.Data;
using Marketplace.DAL.DTOs;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL.Repositories
{
    public class OrderRepository: GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context): base(context)
        {

        }

        public async Task<List<DailySalesStatsDto>> GetBusinessStatByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate) 
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date
                    && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items
                .Where(oi => oi.Product.BusinessId == businessId))
                .GroupBy(oi => oi.CreatedAt.Date).Select(g => new DailySalesStatsDto
                {
                    Date = g.Key,
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                    OrderCount = g.Select(x => x.OrderId).Distinct().Count()
                })
                .ToListAsync();

            return result;
        }

        public async Task<List<ProductStatsByPeriodDto>> GetProductsStatsByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate) 
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date
                    && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items
                .Where(oi => oi.Product.BusinessId == businessId))
                .GroupBy(oi => new { oi.ProductId, oi.Product.Name }).Select(g => new ProductStatsByPeriodDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice),
                    TotalSales = g.Select(x => x.OrderId).Distinct().Count()

                }).ToListAsync();

            return result;
        }

        public async Task<List<CustomerStatsByPeriodDto>> GetCustomersStatsByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate) 
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date
                    && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items
                .Where(oi => oi.Product.BusinessId == businessId))
                .GroupBy(oi => new { oi.Order.UserId, oi.Order.User.UserName })
                .Select(g => new CustomerStatsByPeriodDto {
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName!,
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice),
                    PurchaseCount = g.Select(x => x.OrderId).Distinct().Count()
                }).ToListAsync();

            return result;
        }
    }
}
