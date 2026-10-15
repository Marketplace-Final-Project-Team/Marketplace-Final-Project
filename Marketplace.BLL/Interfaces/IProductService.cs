using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IProductService
    {
        Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null, bool allowAdultContent = false);
        Task<CataloguePageResultDto> GetCataloguePageAsync(CatalogueFilterDto filter, bool allowAdultContent = false);
        Task<Product?> GetCatalogueProductAsync(Guid id, bool allowAdultContent = false);
        Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId);
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(ProductCreateDto dto);
        Task<Product?> UpdateAsync(ProductEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<CatalogProductDto>> GetSearchSuggestionsAsync(string? term, int limit = 8, bool allowAdultContent = false);
        Task<byte[]> ExportToExcelAsync(Guid businessId);
        Task<ProductImportResultDto> ImportFromExcelAsync(Guid businessId, Stream fileStream);
        Task<byte[]> GetImportExampleAsync();
        Task<IReadOnlyList<ReservedProductDto>> GetReservedProductsAsync(Guid businessId);
        Task<IReadOnlyList<StockForecastDto>> GetStockForecastAsync(Guid businessId, int lookbackDays = 30);
        Task<ServiceResult> UpdateQuantityAsync(Guid productId, int quantity);
    }
}