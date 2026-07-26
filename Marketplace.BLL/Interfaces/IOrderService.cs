using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Interfaces
{
    public interface IOrderService
    {
        public Task<ServiceResult> CreateOrder(string userId, OrderCreateDto dto);
        public Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status);
    }
}
