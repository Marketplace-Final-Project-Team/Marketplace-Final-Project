using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;

namespace Marketplace.BLL.Services
{
    public class AddressService : IAddressService
    {
        private readonly IGenericRepository<Address> _addressRepo;

        public AddressService(IGenericRepository<Address> addressRepo)
        {
            _addressRepo = addressRepo;
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
    }
}
