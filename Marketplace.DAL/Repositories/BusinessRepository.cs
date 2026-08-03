using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.DAL.Repositories
{
    public class BusinessRepository: GenericRepository<Business>, IBusinessRepository
    {
        public BusinessRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Business>> GetBusinessesWithOwnerAsync()
        {
            return await _context.Businesses.Include(b => b.Owner).ToListAsync();
        }   
    }
}
