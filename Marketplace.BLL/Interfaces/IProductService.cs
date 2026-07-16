using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId);
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(ProductCreateDto dto);
        Task<Product?> UpdateAsync(ProductEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}