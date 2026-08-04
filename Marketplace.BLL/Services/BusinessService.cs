using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.BLL.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly IGenericRepository<BusinessMember> _businessMemberRepo;
        private readonly IBusinessRepository _businessRepo;
        private readonly IImageStorageService _imageService;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public BusinessService(IGenericRepository<BusinessMember> businessMemberRepo, IBusinessRepository businessRepo, IImageStorageService imageService, UserManager<User> userManager, AppDbContext context)
        {
            _businessMemberRepo = businessMemberRepo;
            _businessRepo = businessRepo;
            _imageService = imageService;
            _userManager = userManager;
            _context = context;
        }

        private async Task<(ServiceResult, BusinessMember?)> VerifyMembershipRequest(string userId, Guid businessId, string token, BusinessMemberRole role) 
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" }, null);
            }

            var isValid = await _userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, $"role-confirm-{role}", token);
            if (!isValid)
            {
                return (new ServiceResult { Succeeded = false, Message = "Невірний токен" }, null);
            }

            var businessMember = await _businessMemberRepo.FirstOrDefaultAsync(bm => bm.UserId == userId && bm.BusinessId == businessId && bm.Role == role);
            if (businessMember == null)
            {
                return (new ServiceResult { Succeeded = false, Message = "Запит на членство не знайдено" }, null);
            }

            return (new ServiceResult { Succeeded = true, Message = string.Empty }, businessMember);
        }

        public async Task<ServiceResult> CreateBusiness(BusinessCreateDto dto)
        {
            if (await _businessRepo.ExistsAsync(b => b.OwnerId == dto.UserId))
            {
                return new ServiceResult { Succeeded = false, Message = "Ви вже маєте бізнес на своєму аккауті" };
            }

            if (await _businessRepo.ExistsAsync(b => b.StoreName == dto.StoreName))
            {
                return new ServiceResult { Succeeded = false, Message = "Це ім'я вже використовується" };
            }

            var logoUrl = await _imageService.UploadImageAsync(dto.Logo);
            if (logoUrl == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити логотип" };
            }

            var bannerUrl = await _imageService.UploadImageAsync(dto.Banner);
            if (bannerUrl == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити банер" };
            }

            var business = new Business
            {
                OwnerId = dto.UserId,
                StoreName = dto.StoreName,
                Description = dto.Description,
                LogoUrl = logoUrl,
                BannerUrl = bannerUrl
            };

            await _businessRepo.AddAsync(business);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<BusinessMemberResult> AddBusinessMember(BusinessMemberCreateDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null)
            {
                return new BusinessMemberResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            if (await _businessMemberRepo.ExistsAsync(bm => bm.UserId == dto.UserId && bm.BusinessId == dto.BusinessId))
            {
                return new BusinessMemberResult { Succeeded = false, Message = "Користувач вже є членом цього бізнесу" };
            }

            var businessMember = new BusinessMember
            {
                UserId = dto.UserId,
                BusinessId = dto.BusinessId,
                Role = dto.Role
            };

            await _businessMemberRepo.AddAsync(businessMember);
            await _context.SaveChangesAsync();

            var token = await _userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, $"role-confirm-{dto.Role}");

            return new BusinessMemberResult { Succeeded = true, Message = string.Empty, UserId = user.Id, Email = user.Email, Token = token, Role = dto.Role };
        }

        public async Task<ServiceResult> ApproveMembership(string userId, Guid businessId, string token, BusinessMemberRole role)
        {
            var (verificationResult, businessMember) = await VerifyMembershipRequest(userId, businessId, token, role);

            if (verificationResult.Succeeded == false)
            {
                return verificationResult;
            }

            businessMember!.IsAccepted = true;
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Запит прийнято" };
        }

        public async Task<ServiceResult> DenyMembership(string userId, Guid businessId, string token, BusinessMemberRole role) 
        {
            var (verificationResult, businessMember) = await VerifyMembershipRequest(userId, businessId, token, role);

            if(verificationResult.Succeeded == false) 
            {
                return verificationResult;
            }

            _businessMemberRepo.Remove(businessMember!);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Запит відхилено" };
        }
    }
}
