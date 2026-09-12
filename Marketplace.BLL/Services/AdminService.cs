using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using AdultContentStatus = Marketplace.Domain.Entities.AdultContentStatus;

namespace Marketplace.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<User> _userManager;
        private readonly IBusinessRepository _businessRepo;
        private readonly IOrderRepository _orderRepository;
        private readonly AppDbContext _context;

        public AdminService(
            IGenericRepository<Product> productRepo,
            UserManager<User> userManager,
            IBusinessRepository businessRepo,
            IOrderRepository orderRepository,
            AppDbContext context)
        {
            _productRepo = productRepo;
            _userManager = userManager;
            _businessRepo = businessRepo;
            _orderRepository = orderRepository;
            _context = context;
        }

        public async Task<IReadOnlyList<ProductAdminDto>> GetProductsAsync()
        {
            var res = (await _productRepo.GetAllAsync()).Select(p => new ProductAdminDto
            {
                Id = p.Id,
                ImageUrl = p.MainImageUrl,
                Name = p.Name,
                Price = p.Price,
                Status = p.Status,
            }).ToList();

            return res;
        }

        public IReadOnlyList<UserAdminDto> GetUsers()
        {
            var res = _userManager.Users.Select(u => new UserAdminDto
            {
                Id = u.Id,
                Email = u.Email!,
                Status = u.Status
            }).ToList();

            return res;
        }

        public async Task<IReadOnlyList<BusinessAdminDto>> GetBusinessesAsync()
        {
            var res = (await _businessRepo.GetBusinessesWithOwnerAsync()).Select(b => new BusinessAdminDto
            {
                Id = b.Id,
                LogoUrl = b.LogoUrl,
                Name = b.StoreName,
                Owner = b.Owner.Email!,
                Status = b.Status,
                CommissionPercent = b.CommissionPercent
            }).ToList();

            return res;
        }

        public async Task<PagedResult<ProductAdminDto>> GetFilteredProductsAsync(ProductAdminQueryDto query)
        {
            var productsQuery = _productRepo.GetQueryable();

            if (query.CategoryId != null)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId);
            }

            if (query.Search != null)
            {
                var normalizedSearch = query.Search.Trim();
                productsQuery = productsQuery.Where(p => p.Name.Contains(normalizedSearch) || p.Business.StoreName.Contains(normalizedSearch));
            }

            if (!string.IsNullOrEmpty(query.SearchBusiness))
            {
                var normalizedBusinessSearch = query.SearchBusiness.Trim();
                productsQuery = productsQuery.Where(p => p.Business.StoreName.Contains(normalizedBusinessSearch));
            }

            if (query.MinPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= query.MaxPrice.Value);
            }

            if (query.InStockOnly == true)
            {
                productsQuery = productsQuery.Where(p => p.Quantity > 0);
            }

            if (query.Status.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Status == query.Status.Value);
            }

            switch (query.SortOption)
            {
                case ProductSortOption.PriceAsc:
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case ProductSortOption.PriceDesc:
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case ProductSortOption.CreatedAtAsc:
                    productsQuery = productsQuery.OrderBy(p => p.CreatedAt);
                    break;
                case ProductSortOption.CreatedAtDesc:
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                case ProductSortOption.PopularityAsc:
                    productsQuery = productsQuery.OrderBy(p => p.OrderItems.Sum(oi => oi.Quantity));
                    break;
                case ProductSortOption.PopularityDesc:
                    productsQuery = productsQuery.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity));
                    break;
            }

            var page = Math.Max(1, query.Pagination.Page);
            var totalCount = await productsQuery.CountAsync();

            var items = await productsQuery.Skip((page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(p => new ProductAdminDto
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.MainImageUrl,
                Price = p.Price,
                Status = p.Status,
                Business = p.Business.StoreName
            }).ToListAsync();

            return new PagedResult<ProductAdminDto>
            {
                Items = items,
                Page = page,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.Pagination.PageSize)
            };
        }

        public async Task<PagedResult<UserAdminDto>> GetFilteredUsersAsync(UserAdminQueryDto query)
        {
            var usersQuery = _userManager.Users;

            if (query.Status != null)
            {
                usersQuery = usersQuery.Where(u => u.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var normalizedSearch = query.Search.Trim();

                usersQuery = usersQuery.Where(u => u.Email!.Contains(normalizedSearch) || u.UserName!.Contains(normalizedSearch));
            }

            usersQuery = query.SortDescending == true ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt);

            var page = Math.Max(1, query.Pagination.Page);
            var totalCount = await usersQuery.CountAsync();

            var items = await usersQuery.Skip((page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(u => new UserAdminDto
            {
                Id = u.Id,
                Email = u.Email!,
                Status = u.Status
            }).ToListAsync();

            return new PagedResult<UserAdminDto>
            {
                Items = items,
                Page = page,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.Pagination.PageSize)
            };
        }

        public async Task<PagedResult<BusinessAdminDto>> GetFilteredBusinessesAsync(BusinessAdminQueryParams query)
        {
            var businessesQuery = _businessRepo.GetQueryable();

            if (query.Status != null)
            {
                businessesQuery = businessesQuery.Where(b => b.Status == query.Status);
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                var normalizedSearch = query.Search.Trim();
                businessesQuery = businessesQuery.Where(b => b.StoreName.Contains(normalizedSearch) || b.Owner.Email!.Contains(normalizedSearch));
            }

            if (!string.IsNullOrEmpty(query.SearchEmail))
            {
                var normalizedEmail = query.SearchEmail.Trim();
                businessesQuery = businessesQuery.Where(b => b.Owner.Email!.Contains(normalizedEmail));
            }

            if (query.MinCommission.HasValue)
            {
                businessesQuery = businessesQuery.Where(b => b.CommissionPercent >= query.MinCommission.Value);
            }

            if (query.MaxCommission.HasValue)
            {
                businessesQuery = businessesQuery.Where(b => b.CommissionPercent <= query.MaxCommission.Value);
            }

            switch (query.SortOption)
            {
                case BusinessSortOption.CreatedAtAsc:
                    businessesQuery = businessesQuery.OrderBy(b => b.CreatedAt);
                    break;
                case BusinessSortOption.CreatedAtDesc:
                    businessesQuery = businessesQuery.OrderByDescending(b => b.CreatedAt);
                    break;
                case BusinessSortOption.CommissionAsc:
                    businessesQuery = businessesQuery.OrderBy(b => b.CommissionPercent);
                    break;
                case BusinessSortOption.CommissionDesc:
                    businessesQuery = businessesQuery.OrderByDescending(b => b.CommissionPercent);
                    break;
            }

            var page = Math.Max(1, query.Pagination.Page);
            var totalCount = await businessesQuery.CountAsync();

            var items = await businessesQuery.Skip((page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(i => new BusinessAdminDto
            {
                Id = i.Id,
                Name = i.StoreName,
                LogoUrl = i.LogoUrl,
                Owner = i.Owner.Email!,
                Status = i.Status,
                CommissionPercent = i.CommissionPercent
            }).ToListAsync();

            return new PagedResult<BusinessAdminDto>
            {
                Items = items,
                Page = page,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.Pagination.PageSize)
            };
        }

        public async Task<ServiceResult> ChangeProductStatus(Guid productId, ProductStatus productStatus)
        {
            var product = await _productRepo.GetByIdAsync(productId);

            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Продукт не знайдено" };
            }

            product.Status = productStatus;
            _productRepo.Update(product);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> ChangeUserStatus(string userId, UserStatus userStatus)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувач не знайдений" };
            }

            user.Status = userStatus;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося оновити статус користувача" };
            }

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> ChangeBusinessStatus(Guid businessId, BusinessStatus businessStatus)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);

            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Бізнес не знайдено" };
            }

            business.Status = businessStatus;
            _businessRepo.Update(business);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<IReadOnlyList<BusinessAdminDto>> GetPendingAdultRequestsAsync()
        {
            var pendingBusinesses = await _context.Businesses
                .Include(b => b.Owner)
                .Where(b => b.AdultStatus == AdultContentStatus.Pending)
                .ToListAsync();

            return pendingBusinesses.Select(b => new BusinessAdminDto
            {
                Id = b.Id,
                Name = b.StoreName,
                LogoUrl = b.LogoUrl,
                Owner = b.Owner.Email!,
                Status = b.Status
            }).ToList();
        }

        public async Task<ServiceResult> ApproveAdultStatusAsync(Guid businessId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Магазин не знайдено" };
            }

            business.AdultStatus = AdultContentStatus.Approved;
            business.AdultRejectionReason = null;

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = "Статус 18+ успішно підтверджено" };
        }

        public async Task<ServiceResult> RejectAdultStatusAsync(Guid businessId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ServiceResult { Succeeded = false, Message = "Причина відхилення обов'язкова" };
            }

            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Магазин не знайдено" };
            }

            business.AdultStatus = AdultContentStatus.Rejected;
            business.AdultRejectionReason = reason;

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = "Заявку на статус 18+ відхилено" };
        }

        public async Task<ServiceResult> ChangeBusinessComission(Guid businessId, decimal commissionPercent)
        {
            var business = await _businessRepo.GetByIdAsync(businessId);
            if (business == null)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Бізнес не знайдено"
                };
            }
            if (commissionPercent < 0 || commissionPercent > 100)
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = "Комісія повинна бути в межах від 0 до 100"
                };
            }
            business.CommissionPercent = commissionPercent;
            await _context.SaveChangesAsync();
            return new ServiceResult
            {
                Succeeded = true,
                Message = "Комісія успішно змінена",
            };
        }

        public async Task<DataResult<AdminCommissionStatsDto>> GetCommissionStatsByPeriodAsync(DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.UtcNow.Date;

            var actualEndDate = endDate ?? today;
            if (endDate.HasValue && actualEndDate.Date > DateTime.UtcNow.Date)
            {
                return new DataResult<AdminCommissionStatsDto>
                {
                    Message = "Кінцева дата знаходиться у майбутньому",
                    Succeeded = false
                };
            }

            var actualStartDate = startDate ?? today.AddDays(-7);
            if (startDate.HasValue && actualStartDate > actualEndDate)
            {
                return new DataResult<AdminCommissionStatsDto>
                {
                    Message = "Початкова дата більша за кінцеву",
                    Succeeded = false
                };
            }

            var daily = await _orderRepository.GetCommissionPerDayByPeriodAsync(actualStartDate, actualEndDate);
            var totalCommissionByPeriod = daily.Sum(d => d.TotalCommission);
            var byBusiness = await _orderRepository.GetCommissionForBusinessByPeriodAsync(actualStartDate, actualEndDate);

            return new DataResult<AdminCommissionStatsDto>
            {
                Message = string.Empty,
                Succeeded = true,
                Data = new AdminCommissionStatsDto
                {
                    Daily = daily,
                    ByBusiness = byBusiness,
                    TotalCommissionByPeriod = totalCommissionByPeriod
                }
            };
        }

        public async Task<decimal> GetTotalCommissionAsync()
        {
            var totalCommission = await _orderRepository.GetTotalCommissionAsync();

            return totalCommission;
        }

        // "Sale" here means the order actually reached Confirmed - that's the only point
        // OrderService.ChangeOrderStatus captures OrderItem.CommissionAmount and credits the
        // balances, so it's the only status range where turnover/revenue numbers here will
        // ever match what Admin/MarketplaceBalance and the seller's own Business/Finance show.
        // Pending orders are excluded even though they're real carts, because no money has
        // actually changed hands yet.
        public async Task<AdminStatisticsOverviewDto> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            var itemsQuery = _context.OrderItems
                .Where(oi => oi.Order.Status == OrderStatus.Confirmed
                    || oi.Order.Status == OrderStatus.Shipped
                    || oi.Order.Status == OrderStatus.Delivered);

            if (startDate.HasValue)
            {
                itemsQuery = itemsQuery.Where(oi => oi.Order.CreatedAt >= startDate.Value.Date);
            }
            if (endDate.HasValue)
            {
                var endExclusive = endDate.Value.Date.AddDays(1);
                itemsQuery = itemsQuery.Where(oi => oi.Order.CreatedAt < endExclusive);
            }

            var rows = await itemsQuery
                .Select(oi => new
                {
                    oi.OrderId,
                    BusinessId = oi.Product.BusinessId,
                    CategoryId = oi.Product.CategoryId,
                    oi.Order.CreatedAt,
                    LineTotal = oi.UnitPrice * oi.Quantity,
                    oi.Quantity,
                    oi.CommissionAmount
                })
                .ToListAsync();

            var businesses = await _context.Businesses.AsNoTracking()
                .Select(b => new { b.Id, b.StoreName, b.LogoUrl, b.Status })
                .ToListAsync();

            var productCounts = await _context.Products.AsNoTracking()
                .GroupBy(p => p.BusinessId)
                .Select(g => new
                {
                    BusinessId = g.Key,
                    Total = g.Count(),
                    Active = g.Count(p => p.Status == ProductStatus.Active)
                })
                .ToListAsync();

            var businessStats = businesses.Select(b =>
            {
                var rowsForBusiness = rows.Where(r => r.BusinessId == b.Id).ToList();
                var productCount = productCounts.FirstOrDefault(p => p.BusinessId == b.Id);

                return new BusinessStatisticsDto
                {
                    BusinessId = b.Id,
                    StoreName = b.StoreName,
                    LogoUrl = b.LogoUrl,
                    ProductCount = productCount?.Total ?? 0,
                    ActiveProductCount = productCount?.Active ?? 0,
                    OrdersCount = rowsForBusiness.Select(r => r.OrderId).Distinct().Count(),
                    UnitsSold = rowsForBusiness.Sum(r => r.Quantity),
                    Turnover = rowsForBusiness.Sum(r => r.LineTotal),
                    MarketplaceRevenue = rowsForBusiness.Sum(r => r.CommissionAmount)
                };
            })
            .OrderByDescending(b => b.MarketplaceRevenue)
            .ToList();

            var categories = await _context.Categories.AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            var categoryStats = rows
                .GroupBy(r => r.CategoryId)
                .Select(g => new CategoryStatisticsDto
                {
                    CategoryId = g.Key,
                    CategoryName = categories.FirstOrDefault(c => c.Id == g.Key)?.Name ?? "Без категорії",
                    OrdersCount = g.Select(r => r.OrderId).Distinct().Count(),
                    UnitsSold = g.Sum(r => r.Quantity),
                    Turnover = g.Sum(r => r.LineTotal)
                })
                .OrderByDescending(c => c.Turnover)
                .ToList();

            var groupByMonth = !startDate.HasValue || !endDate.HasValue
                || (endDate.Value - startDate.Value).TotalDays > 31;

            var byPeriod = rows
                .GroupBy(r => groupByMonth
                    ? new DateTime(r.CreatedAt.Year, r.CreatedAt.Month, 1)
                    : r.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new PeriodStatisticsDto
                {
                    PeriodStart = g.Key,
                    Label = groupByMonth ? g.Key.ToString("MMMM yyyy") : g.Key.ToString("dd.MM.yyyy"),
                    OrdersCount = g.Select(r => r.OrderId).Distinct().Count(),
                    Turnover = g.Sum(r => r.LineTotal),
                    MarketplaceRevenue = g.Sum(r => r.CommissionAmount)
                })
                .ToList();

            return new AdminStatisticsOverviewDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalTurnover = rows.Sum(r => r.LineTotal),
                TotalMarketplaceRevenue = rows.Sum(r => r.CommissionAmount),
                TotalOrders = rows.Select(r => r.OrderId).Distinct().Count(),
                TotalUnitsSold = rows.Sum(r => r.Quantity),
                ActiveBusinessCount = businesses.Count(b => b.Status == BusinessStatus.Active),
                TotalBusinessCount = businesses.Count,
                TotalProductCount = productCounts.Sum(p => p.Total),
                Businesses = businessStats,
                ByPeriod = byPeriod,
                Categories = categoryStats
            };
        }

        public async Task<BusinessAdminDetailsDto?> GetBusinessDetailsAsync(Guid businessId)
        {
            var business = await _context.Businesses
                .Include(b => b.Owner)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                return null;
            }

            var products = await _productRepo.FindAsync(p => p.BusinessId == businessId);

            return new BusinessAdminDetailsDto
            {
                Id = business.Id,
                StoreName = business.StoreName,
                Description = business.Description,
                LogoUrl = business.LogoUrl,
                BannerUrl = business.BannerUrl,
                Status = business.Status,
                CommissionPercent = business.CommissionPercent,
                CreatedAt = business.CreatedAt,
                OwnerEmail = business.Owner.Email ?? string.Empty,
                AdultStatus = business.AdultStatus,
                AdultRejectionReason = business.AdultRejectionReason,
                TotalProductCount = products.Count,
                ActiveProductCount = products.Count(p => p.Status == ProductStatus.Active),
                PendingProductCount = products.Count(p => p.Status == ProductStatus.Pending),
                BannedProductCount = products.Count(p => p.Status == ProductStatus.Banned)
            };
        }
    }
}