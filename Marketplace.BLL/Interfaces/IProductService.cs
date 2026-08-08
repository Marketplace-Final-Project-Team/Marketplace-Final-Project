using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces;

public interface IProductService
{
    Task<PagedResult<CatalogProductDto>> GetCatalogAsync(ProductCatalogQueryDto query);
    Task<Product?> GetCatalogProductAsync(Guid id);
    Task<IReadOnlyList<CatalogProductDto>> GetSearchSuggestionsAsync(string? term, int limit = 8);
    Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId);
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(ProductCreateDto dto);
    Task<Product?> UpdateAsync(ProductEditDto dto);
    Task<bool> DeleteAsync(Guid id);
}