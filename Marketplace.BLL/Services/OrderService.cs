using Marketplace.BLL.Common;
using Marketplace.BLL.Configs;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.DTOs.Businesses;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Pricing;
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
        private readonly IPromoCodeService _promoCodeService;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        public readonly IOptions<LiqPayOptions> _options;
        public readonly IGenericRepository<Business> _businessRepository;

        public OrderService(
            IGenericRepository<Order> orerRepository,
            ICartItemRepository cartItemRepository,
            IGenericRepository<Address> addressRepository,
            IGenericRepository<Product> productRepository,
            AppDbContext context,
            IPaymentService paymentService,
            IPromoCodeService promoCodeService,
            IMarketplaceBalanceService marketplaceBalanceService,
            IOptions<LiqPayOptions> options,
            IGenericRepository<Business> businessRepository)
        {
            _orerRepository = orerRepository;
            _cartItemRepository = cartItemRepository;
            _addressRepository = addressRepository;
            _productRepository = productRepository;
            _paymentService = paymentService;
            _promoCodeService = promoCodeService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _context = context;
            _options = options;
            _businessRepository = businessRepository;
        }

        public async Task<OrderResultDto> CreateOrder(string userId, OrderCreateDto dto)
        {

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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
                var promoCode = !string.IsNullOrWhiteSpace(dto.PromoCode)
                    ? await _promoCodeService.FindActiveAsync(dto.PromoCode)
                    : null;
                var promoCodeApplied = false;
                var now = DateTime.UtcNow;

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

                    var availableStock = cartItem.ProductVariant?.Quantity ?? cartItem.Product.Quantity;
                    if (availableStock < cartItem.Quantity)
                    {
                        return new OrderResultDto { Succeeded = false, Message = $"Недостатньо товару «{cartItem.Product.Name}» на складі." };
                    }
                    var saleDiscountPercent = DiscountCalculator.GetEffectiveDiscountPercent(cartItem.Product, userId, now);
                    var promoApplies = promoCode is not null && promoCode.BusinessId == cartItem.Product.BusinessId;
                    var promoPercent = promoApplies ? promoCode!.Percent : 0;
                    if (promoApplies)
                    {
                        promoCodeApplied = true;
                    }
                    var effectivePercent = saleDiscountPercent + promoPercent;
                    var unitPrice = DiscountCalculator.CalculatePrice(cartItem.Product.Price, effectivePercent);

                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Product.Name,
                        ProductVariantId = cartItem.ProductVariantId,
                        VariantSize = cartItem.ProductVariant?.Size,
                        VariantSku = cartItem.ProductVariant?.Sku ?? cartItem.Product.Sku,
                        UnitPrice = unitPrice,
                        Quantity = cartItem.Quantity,
                        IsReturned = false
                    };

                    if (cartItem.ProductVariant != null)
                    {
                        cartItem.ProductVariant.Quantity -= cartItem.Quantity;
                    }
                    cartItem.Product.Quantity -= cartItem.Quantity;
                    _productRepository.Update(cartItem.Product);

                    orderItems.Add(orderItem);
                }

                var totalAmount = orderItems.Sum(oi => Math.Round(oi.UnitPrice * oi.Quantity, 2, MidpointRounding.AwayFromZero));

                var order = new Order
                {
                    UserId = userId,
                    Items = orderItems,
                    TotalAmount = totalAmount,
                    CreatedAt = DateTime.UtcNow,
                    ShippingCountry = address.Country,
                    ShippingCity = address.City,
                    ShippingStreet = address.Street,
                    Status = OrderStatus.Pending
                };

                await _orerRepository.AddAsync(order);
                _cartItemRepository.RemoveRange(cartItems);

                if (promoCodeApplied && promoCode!.IsSingleUse)
                {
                    promoCode.IsUsed = true;
                }

                await _context.SaveChangesAsync();


                var paymentData = await _paymentService.CreatePaymentAsync(
                    orderId: order.Id.ToString(),
                    amount: order.TotalAmount,
                    description: $"Оплата замовлення #{order.Id}",
                    successUrl: dto.SuccessUrl,
                    cancelUrl: dto.CancelUrl
                );

                await transaction.CommitAsync();

                return new OrderResultDto
                {
                    Succeeded = true,
                    Message = string.Empty,
                    OrderId = order.Id.ToString(),
                    PaymentData = paymentData
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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

                if (status != order.Status && !OrderStatusTransitions.GetAllowedNext(order.Status).Contains(status))
                {
                    return new ServiceResult { Succeeded = false, Message = $"Неможливо змінити статус замовлення з «{order.Status}» на «{status}»." };
                }


                if (status == OrderStatus.Confirmed && order.Status == OrderStatus.Pending)
                {
                    var marketplaceBalance = await _marketplaceBalanceService.GetOrCreateTrackedAsync();

                    foreach (var item in order.Items)
                    {
                        var business = item.Product?.Business;
                        if (business != null)
                        {
                            var amount = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                            var commission = Math.Round(amount * business.CommissionPercent / 100m, 2, MidpointRounding.AwayFromZero);
                            var netAmount = amount - commission;

                            item.CommissionAmount = commission;
                            business.Balance += netAmount;
                            marketplaceBalance.Balance += commission;
                        }
                    }
                }


                if (status == OrderStatus.Canceled && order.Status != OrderStatus.Canceled)
                {
                    foreach (var item in order.Items)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Quantity += item.Quantity;
                        }
                        if (item.ProductVariant != null)
                        {
                            item.ProductVariant.Quantity += item.Quantity;
                        }
                    }


                    if (order.Status is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered)
                    {
                        var marketplaceBalance = await _marketplaceBalanceService.GetOrCreateTrackedAsync();

                        foreach (var item in order.Items)
                        {
                            if (item.Product?.Business != null)
                            {
                                var amount = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                                var netAmount = amount - item.CommissionAmount;

                                item.Product.Business.Balance -= netAmount;
                                marketplaceBalance.Balance -= item.CommissionAmount;
                            }
                        }
                    }
                }

                order.Status = status;
                _orerRepository.Update(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ServiceResult { Succeeded = true, Message = string.Empty };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult> RefundOrderItemAsync(Guid orderItemId)
        {

            try
            {
                var item = await _context.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.Product)
                        .ThenInclude(p => p.Business)
                    .FirstOrDefaultAsync(i => i.Id == orderItemId);

                if (item is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Позицію замовлення не знайдено." };
                }

                if (item.IsReturned)
                {
                    return new ServiceResult { Succeeded = false, Message = "За цей товар уже оформлено повернення." };
                }

                if (item.Order.Status is not (OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered))
                {
                    return new ServiceResult { Succeeded = false, Message = "Повернення можливе лише для підтвердженого або доставленого замовлення." };
                }

                if (item.Product != null)
                {
                    item.Product.Quantity += item.Quantity;

                    if (item.ProductVariant != null)
                    {
                        item.ProductVariant.Quantity += item.Quantity;
                    }

                    if (item.Product.Business != null)
                    {
                        var amount = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                        var netAmount = amount - item.CommissionAmount;
                        var marketplaceBalance = await _marketplaceBalanceService.GetOrCreateTrackedAsync();

                        item.Product.Business.Balance -= netAmount;
                        marketplaceBalance.Balance -= item.CommissionAmount;
                    }
                }

                item.IsReturned = true;
                await _context.SaveChangesAsync();

                return new ServiceResult { Succeeded = true, Message = string.Empty };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId) =>
            await _orerRepository.FirstOrDefaultAsync(o => o.Id == orderId);

        public async Task<IReadOnlyList<Order>> GetUserOrders(string userId) =>
            await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId)
                .Include(order => order.Items)
                .OrderByDescending(order => order.CreatedAt)
                .ToListAsync();

        public async Task<Order?> GetUserOrder(string userId, Guid orderId) =>
            await _context.Orders
                .AsNoTracking()
                .Where(order => order.UserId == userId && order.Id == orderId)
                .Include(order => order.Items)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync();

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

        public async Task<IReadOnlyList<BusinessOrderListItemDto>> GetBusinessOrders(Guid businessId, OrderStatus? statusFilter = null, string? search = null, string? searchEmail = null, decimal? minAmount = null, decimal? maxAmount = null)
        {
            var query = _context.OrderItems
                .AsNoTracking()
                .Where(item => item.Product.BusinessId == businessId);

            if (statusFilter.HasValue)
            {
                query = query.Where(item => item.Order.Status == statusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item => item.Order.User.UserName != null && item.Order.User.UserName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(item => item.Order.User.Email != null && item.Order.User.Email.Contains(searchEmail));
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

            var orders = lines
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
                .AsEnumerable();

            if (minAmount.HasValue)
            {
                orders = orders.Where(o => o.LineTotal >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                orders = orders.Where(o => o.LineTotal <= maxAmount.Value);
            }

            return orders
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
                    VariantSize = item.VariantSize,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    BelongsToBusiness = item.Product != null && item.Product.BusinessId == businessId
                }).ToList()
            };
        }
    }
}