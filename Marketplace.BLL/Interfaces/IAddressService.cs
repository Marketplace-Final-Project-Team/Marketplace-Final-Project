using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IAddressService
    {
        Task<IReadOnlyList<AddressDto>> GetUserAddressesAsync(string userId);
    }
}