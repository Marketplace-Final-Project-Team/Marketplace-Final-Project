using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.DTOs.Businesses;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IOrderService
    {

        Task<OrderResultDto> CreateOrder(string userId, OrderCreateDto dto);
        Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status);
        Task<ServiceResult> RefundOrderItemAsync(Guid orderItemId);
        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task<IReadOnlyList<Order>> GetUserOrders(string userId);
        Task<Order?> GetUserOrder(string userId, Guid orderId);
        Task<BusinessOrderSummaryDto> GetBusinessOrderSummary(Guid businessId);
        Task<IReadOnlyList<BusinessOrderListItemDto>> GetBusinessOrders(Guid businessId, OrderStatus? statusFilter = null, string? search = null, string? searchEmail = null);
        Task<BusinessOrderDetailsDto?> GetBusinessOrderDetails(Guid businessId, Guid orderId);
    }
}
