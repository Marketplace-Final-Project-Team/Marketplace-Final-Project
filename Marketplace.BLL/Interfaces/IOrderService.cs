using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<ServiceResult> CreateOrder(string userId, OrderCreateDto dto);
        Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status);
        Task<IReadOnlyList<Order>> GetUserOrders(string userId);
        Task<Order?> GetUserOrder(string userId, Guid orderId);
    }
}
