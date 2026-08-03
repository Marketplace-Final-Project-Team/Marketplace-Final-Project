using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.DAL.Repositories;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Services
{
    public class BusinessService: IBusinessService
    {
        private readonly IBusinessRepository _businessRepo;
        private readonly IImageStorageService _imageService;
        private readonly AppDbContext _context;

        public BusinessService(IBusinessRepository businessRepo, IImageStorageService imageService, AppDbContext context)
        {
            _businessRepo = businessRepo;
            _imageService = imageService;
            _context = context;
        }

        public async Task<ServiceResult> CreateBusiness(BusinessCreateDto dto) 
        {
            if(await _businessRepo.ExistsAsync(b => b.OwnerId == dto.UserId)) 
            {
                return new ServiceResult { Succeeded = false, Message = "Ви вже маєте бізнес на своєму аккауті" };
            }

            if(await _businessRepo.ExistsAsync(b => b.StoreName == dto.StoreName))
            {
                return new ServiceResult { Succeeded = false, Message = "Це ім'я вже використовується" };
            }

            var logoUrl = await _imageService.UploadImageAsync(dto.Logo);
            if(logoUrl == null)
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
    }
}
