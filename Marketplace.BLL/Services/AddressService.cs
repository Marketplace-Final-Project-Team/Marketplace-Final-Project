using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Marketplace.BLL.Services
{
    public class AddressService : IAddressService
    {
        private readonly IGenericRepository<Address> _addressRepo;
        private readonly UserManager<User> _userManager;
        private readonly AppDbContext _context;

        public AddressService(
            IGenericRepository<Address> addressRepo, 
            UserManager<User> userManager,
            AppDbContext context
            )
        {
            _addressRepo = addressRepo;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IReadOnlyList<AddressDto>> GetUserAddressesAsync(string userId)
        {
            var addresses = await _addressRepo.FindAsync(a => a.UserId == userId);

            return addresses
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    Country = a.Country,
                    City = a.City,
                    Street = a.Street,
                    IsDefault = a.IsDefault
                }).ToList();
        }

        public async Task<AddressDto?> GetByIdAsync(Guid id, string userId) 
        {
            var address = await _addressRepo.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            var res = address == null ? null : new AddressDto
            {
                Id = address.Id,
                Country = address.Country,
                City = address.City,
                Street = address.Street,
                IsDefault = address.IsDefault
            };

            return res;
        }

        public async Task<ServiceResult> CreateAsync(string userId, AddressCreateDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            var address = new Address
            {
                UserId = userId,
                Country = dto.Country,
                City = dto.City,
                Street = dto.Street,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                User = user
            };

            await _addressRepo.AddAsync(address);
            if (dto.IsDefault) await ClearOtherDefaultsAsync(userId, address.Id);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> EditAsync(AddressDto dto, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            var address = await _addressRepo.FirstOrDefaultAsync(a => a.Id == dto.Id && a.UserId == userId);

            if (address == null) 
            {
                return new ServiceResult { Succeeded = false, Message = "Адресу не знайдено" };
            }

            address.Country = dto.Country;
            address.City = dto.City;
            address.Street = dto.Street;
            address.IsDefault = dto.IsDefault;

            _addressRepo.Update(address);
            if (dto.IsDefault) await ClearOtherDefaultsAsync(userId, address.Id);

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> RemoveAsync(Guid id, string userId) 
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            var address = await _addressRepo.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Адресу не знайдено" };
            }

            address.IsDeleted = true;
            address.IsDefault = false;
            _addressRepo.Update(address);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> SetDefaultAddress(Guid id, string userId) 
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Користувача не знайдено" };
            }

            var address = await _addressRepo.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Адресу не знайдено" };
            }

            address.IsDefault = true;
            _addressRepo.Update(address);
            await ClearOtherDefaultsAsync(userId, address.Id);

            await _context.SaveChangesAsync();
            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        private async Task ClearOtherDefaultsAsync(string userId, Guid exceptId)
        {
            var defaults = await _addressRepo.FindAsync(a => a.UserId == userId && a.Id != exceptId && a.IsDefault);
            foreach (var a in defaults)
            {
                a.IsDefault = false;
                _addressRepo.Update(a);
            }
        }

    }
}
