        public async Task<List<DailySalesStatsDto>> GetBusinessStatByPeriodAsync(Guid businessId, DateTime startDate, DateTime endDate) 
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date && o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
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
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date && o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
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
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date && o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
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

        public async Task<List<DailyCommissionStatsDto>> GetCommissionPerDayByPeriodAsync(DateTime startDate, DateTime endDate) 
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date && o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items)
                .GroupBy(oi => oi.CreatedAt.Date).Select(g => new DailyCommissionStatsDto
                {
                    Date = g.Key,
                    TotalCommission = g.Sum(oi => oi.CommissionAmount),
                })
                .OrderByDescending(g => g.TotalCommission)
                .ToListAsync();

            return result;
        }

        public async Task<List<CommissionForBusinessDto>> GetCommissionForBusinessByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            var result = await _context.Orders
                .Where(o => o.CreatedAt.Date >= startDate.Date && o.CreatedAt.Date < endDate.AddDays(1).Date && o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items)
                .GroupBy(oi => new { oi.Product.BusinessId, oi.Product.Business.StoreName }).Select(g => new CommissionForBusinessDto
                {
                    BusinessId = g.Key.BusinessId,
                    BusinessName = g.Key.StoreName,
                    TotalCommission = g.Sum(oi => oi.CommissionAmount),
                })
                .OrderByDescending(g => g.TotalCommission)
                .ToListAsync();

            return result;
        }

        public async Task<decimal> GetTotalCommissionAsync() 
        {
            var totalCommission = await _context.Orders
                .Where(o => o.Status != OrderStatus.Pending && o.Status != OrderStatus.Canceled)
                .SelectMany(o => o.Items)
                .SumAsync(oi => oi.CommissionAmount);

            return totalCommission;
        }