using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IOrderService
    {
        public Task<OrderResultDto> CreateOrder(string userId, OrderCreateDto dto);
        public Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status);

        public Task<Order?> GetOrderByIdAsync(Guid orderId);
        
    }
}
