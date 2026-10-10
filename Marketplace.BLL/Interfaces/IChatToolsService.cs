using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.DAL.DTOs;
using Marketplace.Domain.Entities;

namespace Marketplace.BLL.Interfaces
{
    public interface IChatToolsService
    {
        // Customer
        Task<List<ProductGeminiDto>> SearchProductsAsync(string? searchTerm, string? categoryName, string? businessName, float? minRating, decimal? minPrice, decimal? maxPrice, int limit);
        Task<string[]> GetCategoriesAsync();
        Task<ProductDetailedGeminiDto?> GetProductDetailsAsync(Guid productId);
        Task<List<ProductGeminiDto>> CompareProductsAsync(Guid[] productIds);
        Task<List<ReviewGeminiDto>> GetProductReviewsAsync(Guid productId, int limit);
        Task<List<ProductGeminiDto>> GetSimilarProductsAsync(Guid productId, int limit);
        Task<AvailabilityGeminiDto> CheckAvailabilityAsync(Guid productId);
        Task<List<DiscountGeminiDto>> GetDiscountsAsync(string? categoryName, int limit);
        Task<ServiceResult> AddToCartAsync(Guid productId, int quantity);
        Task<ServiceResult> RemoveFromCartAsync(Guid productId);

        // Business
        Task<BusinessSummaryDto[]> GetMyBusinessesAsync();
        Task<DataResult<SalesSummaryDto>> GetSalesSummaryAsync(Guid businessId, DateTime? startDate, DateTime? endDate);
        Task<DataResult<ProductStatsByPeriodDto[]>> GetProductsSalesStatsAsync(Guid businessId, DateTime? startDate, DateTime? endDate, string sortBy, int limit);
        Task<DataResult<decimal>> GetBusinessBalanceAsync(Guid businessId);
        Task<DataResult<ReviewSummaryDto[]>> GetRecentReviewsAsync(Guid businessId, int limit);
        Task<DataResult<ProductGeminiDto[]>> GetOutOfStockProductsAsync(Guid businessId);
        Task<DataResult<ProductGeminiDto[]>> GetLowStockProductsAsync(Guid businessId, int threshold);
        Task<DataResult<float>> GetAverageRatingForProductAsync(Guid businessId, Guid productId);
        Task<DataResult<object>> GetReviewsForProductAsync(Guid businessId, Guid productId, int limit);
        Task<DataResult<object>> GetBusinessMembersAsync(Guid businessId);
        Task<string> GetWithdrawalInstructionsAsync();
        Task<string> GetModerationPolicySummaryAsync();
        Task<string> GetDiscountSetupInstructionsAsync();
        Task<string> GetBulkImportInstructionsAsync();
    }
}