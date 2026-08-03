using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;

namespace Marketplace.DAL.Interfaces
{
    public interface IBusinessRepository: IGenericRepository<Business>
    {
        public Task<List<Business>> GetBusinessesWithOwnerAsync();
    }
}
