using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Common
{
    // Single source of truth for which OrderStatus can follow which. OrderService enforces
    // this before touching any balance/stock; the seller order-status dropdown uses the same
    // map to only offer legal next statuses in the first place.
    public static class OrderStatusTransitions
    {
        public static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Canceled },
            [OrderStatus.Confirmed] = new[] { OrderStatus.Shipped, OrderStatus.Canceled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered, OrderStatus.Canceled },
            [OrderStatus.Delivered] = new[] { OrderStatus.Canceled },
            [OrderStatus.Canceled] = Array.Empty<OrderStatus>(),
        };

        public static OrderStatus[] GetAllowedNext(OrderStatus current) =>
            Allowed.TryGetValue(current, out var next) ? next : Array.Empty<OrderStatus>();
    }
}
