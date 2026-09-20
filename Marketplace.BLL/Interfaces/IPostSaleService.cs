using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Marketplace.BLL.Interfaces
{
    public interface IPostSaleService
    {
        Task<ServiceResult> CreateRequestAsync(string buyerId, Guid orderItemId, string reason, string comment, List<IFormFile> photos);

        Task<List<PostSaleRequestDto>> GetRequestsForOrderAsync(Guid orderId);

        Task<List<PostSaleRequestDto>> GetRequestsForBusinessAsync(Guid businessId);

        Task<ServiceResult> ResolveRequestAsync(Guid requestId, Guid businessId, PostSaleRequestStatus decision, string? sellerResponse);
        Task<List<PostSaleRequestDto>> GetAllOpenRequestsAsync();

        Task<ServiceResult> AdminApproveAsync(Guid requestId);
        Task<ServiceResult> SellerConfirmReturnAsync(Guid requestId, Guid businessId);

        Task<ServiceResult> AdminRejectAsync(Guid requestId, string reason);
        Task<ServiceResult> SellerDisputeAsync(Guid requestId, Guid businessId, string reason);
    }
}
