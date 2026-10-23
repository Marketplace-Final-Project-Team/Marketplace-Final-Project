using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAddressService
    {
        Task<IReadOnlyList<AddressDto>> GetUserAddressesAsync(string userId);
        Task<AddressDto?> GetByIdAsync(Guid id, string userId);
        Task<ServiceResult> CreateAsync(string userId, AddressCreateDto dto);
        Task<ServiceResult> EditAsync(AddressDto dto, string userId);
        Task<ServiceResult> RemoveAsync(Guid id, string userId);
        Task<ServiceResult> SetDefaultAddress(Guid id, string userId);
    }
}