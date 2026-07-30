using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class OrderService: IOrderService
    {
        private readonly IGenericRepository<Order> _orerRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IGenericRepository<Address> _addressRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly AppDbContext _context;


        public OrderService(IGenericRepository<Order> orerRepository, ICartItemRepository cartItemRepository, IGenericRepository<Address> addressRepository, IGenericRepository<Product> productRepository, AppDbContext context)
        {
            _orerRepository = orerRepository;
            _cartItemRepository = cartItemRepository;
            _addressRepository = addressRepository;
            _productRepository = productRepository;
            _context = context;
        }

        public async Task<ServiceResult> CreateOrder(string userId, OrderCreateDto dto) 
        {
            var cartItems = await _cartItemRepository.GetCartItemsByUserIdAsync(userId);

            if (cartItems == null || !cartItems.Any())
            {
                return new ServiceResult { Succeeded = false, Message = "Кошик пустий." };
            }

            var address = await _addressRepository.FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId);

            if (address == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Адреса не знайдена." };
            }

            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.Product.Status != ProductStatus.Active)
                {
                    return new ServiceResult
                    {
                        Succeeded = false,
                        Message = $"Товар «{cartItem.Product.Name}» більше недоступний."
                    };
                }

                if(cartItem.Product.Quantity < cartItem.Quantity)
                {
                    return new ServiceResult { Succeeded = false, Message = "Недостатньо товару на складі." };
                }

                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    UnitPrice = cartItem.Product.Price,
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
            };

            await _orerRepository.AddAsync(order);
            _cartItemRepository.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = string.Empty,
                EntityId = order.Id
            };
        }

        public async Task<ServiceResult> ChangeOrderStatus(Guid orderId, OrderStatus status)
        {
            var order = await _orerRepository.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Замовлення не знайдено." };
            }

            order.Status = status;
            _orerRepository.Update(order);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
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
    }
}
