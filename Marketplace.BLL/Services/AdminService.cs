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
        private readonly AppDbContext _context;

        public AdminService(
            IGenericRepository<Product> productRepo,
            UserManager<User> userManager,
            IBusinessRepository businessRepo,
            AppDbContext context)
        {
            _productRepo = productRepo;
            _userManager = userManager;
            _businessRepo = businessRepo;
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
                productsQuery = productsQuery.Where(p => p.Name.Contains(normalizedSearch));
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
                Status = p.Status
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
                businessesQuery = businessesQuery.Where(b => b.StoreName.Contains(normalizedSearch));
            }

            businessesQuery = query.SortDescending == true ? businessesQuery.OrderByDescending(b => b.CreatedAt) : businessesQuery.OrderBy(b => b.CreatedAt);

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
    }
}