using Marketplace.BLL.Common;
using Marketplace.BLL.Configs;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.DTOs.Businesses;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IGenericRepository<Order> _orerRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IGenericRepository<Address> _addressRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        public readonly IOptions<LiqPayOptions> _options;


        public OrderService(IGenericRepository<Order> orerRepository, ICartItemRepository cartItemRepository, IGenericRepository<Address> addressRepository, IGenericRepository<Product> productRepository, AppDbContext context, IPaymentService paymentService, IOptions<LiqPayOptions> options)
        {
            _orerRepository = orerRepository;
            _cartItemRepository = cartItemRepository;
            _addressRepository = addressRepository;
            _productRepository = productRepository;
            _paymentService = paymentService;
            _context = context;
            _options = options;
        }

        public async Task<OrderResultDto> CreateOrder(string userId, OrderCreateDto dto)
        {
            var cartItems = await _cartItemRepository.GetCartItemsByUserIdAsync(userId);

            if (cartItems == null || !cartItems.Any())
            {
                return new OrderResultDto { Succeeded = false, Message = "Кошик пустий." };
            }

            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId);

            if (address == null)
            {
                return new OrderResultDto { Succeeded = false, Message = "Адреса не знайдена." };
            }

            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cartItems)
            {

                if (cartItem.Product.Status != ProductStatus.Active)
                {
                    return new OrderResultDto
                    {
                        Succeeded = false,
                        Message = $"Товар «{cartItem.Product.Name}» більше недоступний."
                    };
                }

                if (cartItem.Product.Quantity < cartItem.Quantity)

                {
                    return new OrderResultDto { Succeeded = false, Message = "Недостатньо товару на складі." };
                }

                var now = DateTime.UtcNow;
                var activeDiscount = cartItem.Product.Discounts?
                    .FirstOrDefault(d => d.StartDate <= now && d.EndDate >= now);

                var unitPrice = activeDiscount != null
                    ? cartItem.Product.Price - (cartItem.Product.Price * activeDiscount.Percent / 100m)
                    : cartItem.Product.Price;

                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    UnitPrice = unitPrice,
                    Quantity = cartItem.Quantity
                };

                cartItem.Product.Quantity -= cartItem.Quantity;
                _productRepository.Update(cartItem.Product);

                orderItems.Add(orderItem);
            }

            var order = new Order
            {
                UserId = userId,
                Items = orderItems,
                TotalAmount = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                CreatedAt = DateTime.UtcNow,
                ShippingCountry = address.Country,
                ShippingCity = address.City,
                ShippingStreet = address.Street,
                Status = OrderStatus.Pending
            };

            await _orerRepository.AddAsync(order);
            _cartItemRepository.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            var paymentData = await _paymentService.CreatePaymentAsync(
            orderId: order.Id.ToString(),
            amount: order.TotalAmount,
            description: $"Оплата замовлення #{order.Id}",
            successUrl: dto.SuccessUrl,
            cancelUrl: dto.CancelUrl

            );

            return new OrderResultDto
            {
                Succeeded = true,
                Message = string.Empty,
                OrderId = order.Id.ToString(),
                PaymentData = paymentData
            };
        }


        public async Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Business)
                .FirstOrDefaultAsync(o => o.Id == orderId);



            if (order == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Замовлення не знайдено." };
            }
            if (status == OrderStatus.Confirmed && order.Status != OrderStatus.Confirmed) {
                foreach (var item in order.Items) {
                    if (item.Product.Business != null)
                    {
                        var business = item.Product.Business;
                        if (business == null)
                        {
                            return new ServiceResult { Succeeded = false, Message = "Бізнес не знайдено." };
                        }
                        var amount = item.UnitPrice * item.Quantity;
                        business.Balance += amount; 
                    }
                }
            }

            order.Status = status;
            _orerRepository.Update(order);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _orerRepository.FirstOrDefaultAsync(o => o.Id == orderId);
        }
        public async Task<IReadOnlyList<Order>> GetUserOrders(string userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .Include(order => order.Items)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order?> GetUserOrder(string userId, Guid orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId && order.Id == orderId)
                .Include(order => order.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync();
        }

        public async Task<BusinessOrderSummaryDto> GetBusinessOrderSummary(Guid businessId)
        {
            var orderLines = await _context.OrderItems
                .AsNoTracking()
                .Where(item => item.Product.BusinessId == businessId)
                .Select(item => new
                {
                    item.OrderId,
                    item.Order.Status,
                    item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToListAsync();

            var orders = orderLines
                .GroupBy(line => line.OrderId)
                .Select(group => group.First())
                .ToList();

            return new BusinessOrderSummaryDto
            {
                TotalOrders = orders.Count,
                AwaitingProcessingOrders = orders.Count(order =>
                    order.Status is OrderStatus.Pending or OrderStatus.Confirmed),
                SoldItems = orderLines
                    .Where(line => line.Status != OrderStatus.Canceled)
                    .Sum(line => line.Quantity),
                Revenue = orderLines
                    .Where(line => line.Status != OrderStatus.Canceled)
                    .Sum(line => line.LineTotal)
            };
        }

        public async Task<IReadOnlyList<BusinessOrderListItemDto>> GetBusinessOrders(Guid businessId, OrderStatus? statusFilter = null)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(item => item.Product.BusinessId == businessId);

            if (statusFilter.HasValue)
            {
                query = query.Where(item => item.Order.Status == statusFilter.Value);
            }

            var lines = await query
                .Select(item => new
                {
                    item.OrderId,
                    item.Order.CreatedAt,
                    item.Order.Status,
                    item.Order.ShippingCountry,
                    item.Order.ShippingCity,
                    item.Order.ShippingStreet,
                    BuyerName = item.Order.User.UserName,
                    BuyerEmail = item.Order.User.Email,
                    item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                })
                .ToListAsync();

            return lines
                .GroupBy(line => line.OrderId)
                .Select(group => new BusinessOrderListItemDto
                {
                    OrderId = group.Key,
                    CreatedAt = group.First().CreatedAt,
                    Status = group.First().Status,
                    BuyerName = group.First().BuyerName ?? string.Empty,
                    BuyerEmail = group.First().BuyerEmail ?? string.Empty,
                    ShippingCountry = group.First().ShippingCountry,
                    ShippingCity = group.First().ShippingCity,
                    ShippingStreet = group.First().ShippingStreet,
                    ItemsCount = group.Sum(line => line.Quantity),
                    LineTotal = group.Sum(line => line.LineTotal)
                })
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }

        public async Task<BusinessOrderDetailsDto?> GetBusinessOrderDetails(Guid businessId, Guid orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

  
            if (order == null || !order.Items.Any(item => item.Product != null && item.Product.BusinessId == businessId))
            {
                return null;
            }

            return new BusinessOrderDetailsDto
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                OrderTotalAmount = order.TotalAmount,
                BuyerName = order.User.UserName ?? string.Empty,
                BuyerEmail = order.User.Email ?? string.Empty,
                BuyerPhone = order.User.PhoneNumber ?? string.Empty,
                ShippingCountry = order.ShippingCountry,
                ShippingCity = order.ShippingCity,
                ShippingStreet = order.ShippingStreet,
                Items = order.Items.Select(item => new BusinessOrderLineDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    ImageUrl = item.Product?.MainImageUrl ?? string.Empty,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BelongsToBusiness = item.Product != null && item.Product.BusinessId == businessId
                }).ToList()
            };
        }
    }
}