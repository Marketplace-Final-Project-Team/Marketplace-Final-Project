using System.ComponentModel.DataAnnotations;
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

            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true)
                || (dto.DeliveryCarrier == "NovaPoshta"
                    && (string.IsNullOrWhiteSpace(dto.NovaPoshtaCityRef) || string.IsNullOrWhiteSpace(dto.NovaPoshtaBranchRef))))
            {
                return new OrderResultDto { Succeeded = false, Message = "Перевірте дані отримувача та доставки." };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cartItems = await _cartItemRepository.GetCartItemsByUserIdAsync(userId);

                if (cartItems == null || !cartItems.Any())
                {
                    return new OrderResultDto { Succeeded = false, Message = "Кошик пустий." };
                }

                var orderItems = new List<OrderItem>();
                var promoCode = !string.IsNullOrWhiteSpace(dto.PromoCode)
                    ? await _promoCodeService.FindActiveAsync(dto.PromoCode)
                    : null;
                var promoCodeApplied = false;

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
                    var business = await _businessRepository.FirstOrDefaultAsync(b => b.Id == cartItem.Product.BusinessId);
                    var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == cartItem.Product.CategoryId);


                    var now = DateTime.UtcNow;
                    var productDiscount = cartItem.Product.Discounts?
                        .FirstOrDefault(d => d.ProductId == cartItem.Product.Id && d.DiscountType == DiscountType.Product && d.StartDate <= now && d.EndDate >= now);

                    var businessDiscount = business?.Discounts?
                        .FirstOrDefault(d => d.BusinessId == cartItem.Product.BusinessId && d.DiscountType == DiscountType.Business && d.StartDate <= now && d.EndDate >= now);

                    var categoryDiscount = category?.Discounts?
                        .FirstOrDefault(d => d.DiscountType == DiscountType.Category && d.StartDate <= now && d.BusinessId == cartItem.Product.BusinessId && d.CategoryId == cartItem.Product.CategoryId && d.EndDate >= now);

                    var userdiscount = await _context.UserDiscounts
                        .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.BusinessId == cartItem.Product.BusinessId && ud.StartDate <= DateTime.UtcNow && ud.EndDate >= DateTime.UtcNow);

                    decimal? bestDiscount = 0;
                    if (userdiscount != null)
                    {
                        var alldiscounts = new List<decimal>();
                        if (productDiscount != null)
                        {
                            alldiscounts.Add(productDiscount.Percent);
                        }
                        if (categoryDiscount != null)
                        {
                            alldiscounts.Add(categoryDiscount.Percent);
                        }
                        if (userdiscount != null)
                        {
                            alldiscounts.Add(userdiscount.Percent);
                        }
                        if (businessDiscount != null)
                        {
                            alldiscounts.Add(businessDiscount.Percent);
                        }
                        bestDiscount = alldiscounts.Any() ? alldiscounts.Max() : 0;
                    }
                    else
                    {
                        if (productDiscount != null)
                        {
                            bestDiscount = productDiscount.Percent;
                        }
                        else if (categoryDiscount != null)
                        {
                            bestDiscount = categoryDiscount.Percent;

                        }

                        else if (businessDiscount != null)
                        {
                            bestDiscount = businessDiscount.Percent;
                        }
                    }



                    var saleDiscountPercent = bestDiscount ?? 0;
                    var promoApplies = promoCode is not null && promoCode.BusinessId == cartItem.Product.BusinessId;
                    var promoPercent = promoApplies ? promoCode!.Percent : 0;
                    if (promoApplies)
                    {
                        promoCodeApplied = true;
                    }
                    var effectivePercent = saleDiscountPercent + promoPercent;
                    if (effectivePercent > 100)
                    {
                        effectivePercent = 100;
                    }




                    var unitPrice = effectivePercent > 0
                        ? Math.Round(cartItem.Product.Price * (1m - effectivePercent / 100m), 2, MidpointRounding.AwayFromZero)
                        : Math.Round(cartItem.Product.Price, 2, MidpointRounding.AwayFromZero);

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
                    RecipientLastName = dto.RecipientLastName.Trim(),
                    RecipientFirstName = dto.RecipientFirstName.Trim(),
                    RecipientPhone = dto.RecipientPhone.Trim(),
                    ShippingCountry = "Україна",
                    ShippingCity = dto.ShippingCity.Trim(),
                    ShippingStreet = dto.ShippingStreet.Trim(),
                    DeliveryCarrier = dto.DeliveryCarrier,
                    PaymentMethod = dto.PaymentMethod,
                    NovaPoshtaCityRef = dto.NovaPoshtaCityRef?.Trim(),
                    NovaPoshtaBranchRef = dto.NovaPoshtaBranchRef?.Trim(),
                    Status = OrderStatus.Pending
                };

                await _orerRepository.AddAsync(order);
                _cartItemRepository.RemoveRange(cartItems);

                if (promoCodeApplied && promoCode!.IsSingleUse)
                {
                    promoCode.IsUsed = true;
                }

                await _context.SaveChangesAsync();


                var paymentData = dto.PaymentMethod == "CashOnDelivery" ? null : await _paymentService.CreatePaymentAsync(
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
                            business.HeldBalance += netAmount;
                            item.HeldAmount = netAmount;
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

                                if (item.HeldAmount.HasValue && !item.IsBalanceReleased)
                                {
                                    item.Product.Business.HeldBalance -= item.HeldAmount.Value;
                                    item.IsBalanceReleased = true;
                                }
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
                        if (item.HeldAmount.HasValue && !item.IsBalanceReleased)
                        {
                            item.Product.Business.HeldBalance -= item.HeldAmount.Value;
                            item.IsBalanceReleased = true;
                        }
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
                BuyerName = string.IsNullOrWhiteSpace(order.RecipientLastName)
                    ? order.User.UserName ?? string.Empty
                    : $"{order.RecipientLastName} {order.RecipientFirstName}",
                BuyerEmail = order.User.Email ?? string.Empty,
                BuyerPhone = string.IsNullOrWhiteSpace(order.RecipientPhone)
                    ? order.User.PhoneNumber ?? string.Empty : order.RecipientPhone,
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