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

        public async Task<IReadOnlyList<ProductAdminDto>> GetAllProducts()
        {
            var products = await _productRepo.GetAllAsync();

            return products.Select(p => new ProductAdminDto
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.MainImageUrl,
                Price = p.Price,
                Status = p.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<UserAdminDto>> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            return users.Select(u => new UserAdminDto
            {
                Id = u.Id,
                Email = u.Email!,
                Status = u.Status
            }).ToList();
        }

        public async Task<IReadOnlyList<BusinessAdminDto>> GetAllBusinesses()
        {
            var businesses = await _businessRepo.GetBusinessesWithOwnerAsync();

            return businesses.Select(b => new BusinessAdminDto
            {
                Id = b.Id,
                Name = b.StoreName,
                LogoUrl = b.LogoUrl,
                Owner = b.Owner.Email!,
                Status = b.Status
            }).ToList();
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
    }
}