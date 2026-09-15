using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null, bool allowAdultContent = false);
        Task<CatalogPageResultDto> GetCatalogPageAsync(CatalogFilterDto filter, bool allowAdultContent = false);
        Task<Product?> GetCatalogProductAsync(Guid id, bool allowAdultContent = false);
        Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId);
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(ProductCreateDto dto);
        Task<Product?> UpdateAsync(ProductEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<CatalogProductDto>> GetSearchSuggestionsAsync(string? term, int limit = 8, bool allowAdultContent = false);
        Task<byte[]> ExportToExcelAsync(Guid businessId);
        Task<ProductImportResultDto> ImportFromExcelAsync(Guid businessId, Stream fileStream);
    }
}