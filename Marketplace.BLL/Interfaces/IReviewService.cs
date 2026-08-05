using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface IReviewService
    {
        Task<ServiceResult> CreateReview(ReviewCreateDto dto);
        Task<ServiceResult> UpdateReview(ReviewUpdateDto dto);
        Task<ServiceResult> DeleteReview(Guid reviewId, string userId);
        Task<IReadOnlyList<ReviewDto>> GetReviewsByProductId(Guid productId);
        Task<ReviewUpdateDto?> GetReviewUpdateDtoAsync(Guid reviewId, string userId);
    }
}
