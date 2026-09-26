using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IChatToolsService
    {
        Task<List<ProductGeminiDto>> SearchProductsAsync(string? searchTerm, string? categoryName, string? businessName, float? minRating, decimal? minPrice, decimal? maxPrice, int limit);
        Task<ProductDetailedGeminiDto?> GetProductDetailsAsync(Guid productId);
        Task<List<ProductGeminiDto>> CompareProductsAsync(Guid[] productIds);
        Task<List<ReviewGeminiDto>> GetProductReviewsAsync(Guid productId, int limit);
        Task<List<ProductGeminiDto>> GetSimilarProductsAsync(Guid productId, int limit);
        Task<AvailabilityGeminiDto> CheckAvailabilityAsync(Guid productId);
        Task<List<DiscountGeminiDto>> GetDiscountsAsync(string? categoryName, int limit);
        Task<ServiceResult> AddToCartAsync(Guid productId, int quantity);
        Task<ServiceResult> RemoveFromCartAsync(Guid productId);
        Task<string[]> GetCategoriesAsync();
    }
}