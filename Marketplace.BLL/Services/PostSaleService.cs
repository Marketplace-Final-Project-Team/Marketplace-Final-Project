using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class PostSaleService : IPostSaleService
    {
        private const int MinPhotos = 1;
        private const int MaxPhotos = 5;
        private const int MinCommentLength = 10;

        private readonly AppDbContext _context;
        private readonly IImageStorageService _imageStorageService;
        private readonly IPaymentService _paymentService;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        private readonly IOrderService _orderService;

        public PostSaleService(
             AppDbContext context,
             IImageStorageService imageStorageService,
             IPaymentService paymentService,
             IMarketplaceBalanceService marketplaceBalanceService,
             IOrderService orderService)
        {
            _context = context;
            _imageStorageService = imageStorageService;
            _paymentService = paymentService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _orderService = orderService;
        }

        public async Task<ServiceResult> CreateRequestAsync(string buyerId, Guid orderItemId, string reason, string comment, List<IFormFile> photos)
        {

            var orderItem = await _context.OrderItems
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == orderItemId);

            if (orderItem is null || orderItem.Order.UserId != buyerId)
            {
                return new ServiceResult { Succeeded = false, Message = "Товар у замовленні не знайдено" };
            }

            if (orderItem.Order.Status != OrderStatus.Delivered)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку можна подати лише для доставленого замовлення" };
            }

            var baseDate = orderItem.Order.UpdatedAt ?? orderItem.Order.CreatedAt;
            var timePassed = DateTime.UtcNow - baseDate;


            if (timePassed.TotalDays > 14)
            {
                return new ServiceResult { Succeeded = false, Message = "Термін повернення товару сплив (минуло більше 14 днів з моменту доставки)." };
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ServiceResult { Succeeded = false, Message = "Оберіть причину повернення" };
            }

            if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < MinCommentLength)
            {
                return new ServiceResult { Succeeded = false, Message = $"Опишіть ситуацію детальніше - щонайменше {MinCommentLength} символів" };
            }

            var validPhotos = photos?.Where(p => p.Length > 0).ToList() ?? new List<IFormFile>();
            if (validPhotos.Count < MinPhotos)
            {
                return new ServiceResult { Succeeded = false, Message = "Додайте хоча б одне фото товару" };
            }

            if (validPhotos.Count > MaxPhotos)
            {
                return new ServiceResult { Succeeded = false, Message = $"Можна додати не більше {MaxPhotos} фото" };
            }

            var alreadyExists = await _context.PostSaleRequests
                .AnyAsync(r => r.OrderItemId == orderItemId);

            if (alreadyExists)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку на повернення для цього товару вже подано" };
            }

            var photoUrls = new List<string>();
            foreach (var photo in validPhotos)
            {
                var url = await _imageStorageService.UploadImageAsync(photo);
                if (url is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Не вдалося завантажити одне з фото. Спробуйте ще раз." };
                }
                photoUrls.Add(url);
            }

            var request = new PostSaleRequest
            {
                OrderId = orderItem.OrderId,
                OrderItemId = orderItemId,
                BuyerId = buyerId,
                Reason = reason,
                Comment = comment.Trim(),
                Status = PostSaleRequestStatus.Pending,
                Images = photoUrls.Select(url => new PostSaleRequestImage { ImageUrl = url }).ToList()
            };

            await _context.PostSaleRequests.AddAsync(request);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Заявку подано", EntityId = request.Id };
        }

        public async Task<List<PostSaleRequestDto>> GetRequestsForOrderAsync(Guid orderId)
        {
            var requests = await _context.PostSaleRequests
                .Include(r => r.Buyer)
                .Include(r => r.Images)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(r => r.OrderId == orderId)
                .ToListAsync();

            return requests.Select(ToDto).ToList();
        }

        public async Task<List<PostSaleRequestDto>> GetRequestsForBusinessAsync(Guid businessId)
        {
            var requests = await _context.PostSaleRequests
                .Include(r => r.Buyer)
                .Include(r => r.Images)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(r => r.OrderItem.Product != null && r.OrderItem.Product.BusinessId == businessId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(ToDto).ToList();
        }

        public async Task<ServiceResult> ResolveRequestAsync(Guid requestId, Guid businessId, PostSaleRequestStatus decision, string? sellerResponse)
        {
            if (decision == PostSaleRequestStatus.Pending)
            {
                return new ServiceResult { Succeeded = false, Message = "Некоректне рішення" };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = await _context.PostSaleRequests
                    .Include(r => r.OrderItem)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Business)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request is null)
                {
                    return new ServiceResult { Succeeded = false, Message = "Заявку не знайдено" };
                }

                if (request.OrderItem.Product is null || request.OrderItem.Product.BusinessId != businessId)
                {
                    return new ServiceResult { Succeeded = false, Message = "Цей товар не належить вашому магазину" };
                }

                if (request.Status != PostSaleRequestStatus.Pending)
                {
                    return new ServiceResult { Succeeded = false, Message = "Заявку вже розглянуто" };
                }

                request.SellerResponse = sellerResponse;
                request.ResolvedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                if (decision == PostSaleRequestStatus.Approved)
                {
                    decimal totalRefundAmount = request.OrderItem.UnitPrice * request.OrderItem.Quantity;
                    bool paymentRefunded = await _paymentService.RefundPaymentAsync(request.OrderId.ToString(), totalRefundAmount);

                    if (!paymentRefunded)
                    {
                        await transaction.RollbackAsync();
                        return new ServiceResult { Succeeded = false, Message = "Не вдалося провести повернення коштів через платіжну систему" };
                    }


                    var stockResult = await _orderService.RefundOrderItemAsync(request.OrderItemId);
                    if (!stockResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new ServiceResult
                        {
                            Succeeded = false,
                            Message = $"Кошти повернуто через платіжну систему, але виникла помилка синхронізації складу/балансу: {stockResult.Message}"
                        };
                    }

                    request.Status = PostSaleRequestStatus.Refunded;
                }
                else
                {
                    request.Status = decision;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ServiceResult { Succeeded = true, Message = "Рішення збережено та оброблено", EntityId = request.OrderItemId };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static PostSaleRequestDto ToDto(PostSaleRequest r) => new()
        {
            Id = r.Id,
            OrderId = r.OrderId,
            OrderItemId = r.OrderItemId,
            ProductId = r.OrderItem?.ProductId ?? Guid.Empty,
            ProductName = r.OrderItem?.ProductName ?? string.Empty,
            ImageUrl = r.OrderItem?.Product?.MainImageUrl ?? string.Empty,
            UnitPrice = r.OrderItem?.UnitPrice ?? 0,
            Quantity = r.OrderItem?.Quantity ?? 0,
            BuyerId = r.BuyerId,
            BuyerName = r.Buyer != null ? (r.Buyer.UserName ?? r.Buyer.Email ?? string.Empty) : string.Empty,
            BuyerEmail = r.Buyer != null ? (r.Buyer.Email ?? string.Empty) : string.Empty,
            Reason = r.Reason,
            Comment = r.Comment,
            PhotoUrls = r.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
            Status = r.Status,
            SellerResponse = r.SellerResponse,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt
        };
    }
}