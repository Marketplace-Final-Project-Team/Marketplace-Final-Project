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

namespace Marketplace.BLL.Services
{
    public class AdminService: IAdminService
    {
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<User> _userManager;
        private readonly IBusinessRepository _businessRepo;
        private readonly AppDbContext _context;

        public AdminService(IGenericRepository<Product> productRepo, UserManager<User> userManager, IBusinessRepository businessRepo, AppDbContext context)
        {
            _productRepo = productRepo;
            _userManager = userManager;
            _businessRepo = businessRepo;
            _context = context;
        }

        public async Task<PagedResult<ProductAdminDto>> GetProductsAsync(ProductAdminQueryDto query)
        {
            var productsQuery = _productRepo.GetQueryable();

            if(query.CategoryId != null)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == query.CategoryId);
            }

            if(query.Search != null)
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query.Search));
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

            var totalCount = await productsQuery.CountAsync();

            var items = await productsQuery.Skip((query.Pagination.Page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(p => new ProductAdminDto
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
                Page = query.Pagination.Page,
                PageSize = query.Pagination.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<UserAdminDto>> GetUsersAsync(UserAdminQueryDto query)
        {
            var usersQuery = _userManager.Users;

            if (query.Status != null)
            {
                usersQuery = usersQuery.Where(u => u.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                usersQuery = usersQuery.Where(u => u.Email!.Contains(query.Search) || u.UserName!.Contains(query.Search));
            }

            usersQuery = query.SortDescending == true ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt);

            var totalCount = await usersQuery.CountAsync();

            var items = await usersQuery.Skip((query.Pagination.Page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(u => new UserAdminDto
            {
                Id = u.Id,
                Email = u.Email!,
                Status = u.Status
            }).ToListAsync();

            return new PagedResult<UserAdminDto>
            {
                Items = items,
                Page = query.Pagination.Page,
                PageSize = query.Pagination.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<BusinessAdminDto>> GetBusinessesAsync(BusinessAdminQueryParams query)
        {
            var businessesQuery = _businessRepo.GetQueryable();

            if(query.Status != null) 
            {
                businessesQuery = businessesQuery.Where(b => b.Status == query.Status);
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                businessesQuery = businessesQuery.Where(b => b.StoreName.Contains(query.Search));
            }

            businessesQuery = query.SortDescending == true ? businessesQuery.OrderByDescending(b => b.CreatedAt) : businessesQuery.OrderBy(b => b.CreatedAt);

            var totalCount = await businessesQuery.CountAsync();

            var items = await businessesQuery.Skip((query.Pagination.Page - 1) * query.Pagination.PageSize).Take(query.Pagination.PageSize).Select(i => new BusinessAdminDto
            {
                Id = i.Id,
                Name = i.StoreName,
                LogoUrl = i.LogoUrl,
                Owner = i.Owner.Email!,
                Status = i.Status
            }).ToListAsync();

            return new PagedResult<BusinessAdminDto> 
            {
                Items = items,
                Page = query.Pagination.Page,
                PageSize = query.Pagination.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<ServiceResult> ChangeProductStatus(Guid productId, ProductStatus productStatus)
        {
            var product = await _productRepo.GetByIdAsync(productId);
            
            if (product == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Продукт не найден" };
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
    }
}
