using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Jobs;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Quartz;

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
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly INotificationPusher _notificationPusher;

        public PostSaleService(
             AppDbContext context,
             IImageStorageService imageStorageService,
             IPaymentService paymentService,
             IMarketplaceBalanceService marketplaceBalanceService,
             IOrderService orderService,
             ISchedulerFactory schedulerFactory,
             INotificationPusher notificationPusher)
        {
            _context = context;
            _imageStorageService = imageStorageService;
            _paymentService = paymentService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _orderService = orderService;
            _schedulerFactory = schedulerFactory;
            _notificationPusher = notificationPusher;
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

            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey($"postsale-ai-moderation-job-{request.Id}", "postsale-moderation");

            var job = JobBuilder.Create<PostSaleRequestModerationJob>()
                .WithIdentity(jobKey)
                .UsingJobData("RequestId", request.Id.ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"postsale-ai-moderation-trigger-{request.Id}", "postsale-moderation")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
                .Build();

            await scheduler.ScheduleJob(job, trigger);

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


        public async Task<ServiceResult> AdminApproveAsync(Guid requestId)
        {
            var request = await _context.PostSaleRequests.FirstOrDefaultAsync(r => r.Id == requestId);

            if (request is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку не знайдено" };
            }

            if (request.Status != PostSaleRequestStatus.Pending && request.Status != PostSaleRequestStatus.SellerDisputed)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку вже опрацьовано" };
            }

            var isFirstApproval = request.SellerConfirmDeadline is null;

            request.Status = PostSaleRequestStatus.AwaitingSellerConfirmation;
            request.SellerConfirmDeadline ??= DateTime.UtcNow.AddDays(7);
            request.SellerDisputeReason = null;
            request.UpdatedAt = DateTime.UtcNow;
            request.StatusChangedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

          
            if (isFirstApproval)
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey($"postsale-deadline-job-{request.Id}", "postsale");

                var job = JobBuilder.Create<PostSaleAutoResolveJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("RequestId", request.Id.ToString())
                    .Build();

                var trigger = TriggerBuilder.Create()
                    .WithIdentity($"postsale-deadline-trigger-{request.Id}", "postsale")
                    .StartAt(new DateTimeOffset(request.SellerConfirmDeadline!.Value, TimeSpan.Zero))
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }

            await _notificationPusher.PushToUserAsync(request.BuyerId,
                "Адміністратор підтвердив вашу заявку на повернення — очікуємо продавця.", "ReturnStatus");

            return new ServiceResult { Succeeded = true, Message = "Заявку підтверджено. Очікуємо підтвердження продавця.", EntityId = request.Id };
        }

        public async Task<ServiceResult> AdminRejectAsync(Guid requestId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ServiceResult { Succeeded = false, Message = "Вкажіть причину відхилення" };
            }

            var request = await _context.PostSaleRequests.FirstOrDefaultAsync(r => r.Id == requestId);

            if (request is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку не знайдено" };
            }

            if (request.Status != PostSaleRequestStatus.Pending && request.Status != PostSaleRequestStatus.SellerDisputed)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку вже опрацьовано" };
            }

            request.Status = PostSaleRequestStatus.Rejected;
            request.SellerResponse = reason.Trim();
            request.ResolvedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.StatusChangedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var scheduler2 = await _schedulerFactory.GetScheduler();
            await scheduler2.DeleteJob(new JobKey($"postsale-deadline-job-{request.Id}", "postsale"));

            await _notificationPusher.PushToUserAsync(request.BuyerId,
                "У вашій заявці на повернення відмовлено.", "ReturnStatus");

            return new ServiceResult { Succeeded = true, Message = "Заявку відхилено.", EntityId = request.Id };
        }

        public async Task<ServiceResult> SellerDisputeAsync(Guid requestId, Guid businessId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ServiceResult { Succeeded = false, Message = "Вкажіть причину відмови" };
            }

            var request = await _context.PostSaleRequests
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request is null)
            {
                return new ServiceResult { Succeeded = false, Message = "Заявку не знайдено" };
            }

            if (request.OrderItem.Product is null || request.OrderItem.Product.BusinessId != businessId)
            {
                return new ServiceResult { Succeeded = false, Message = "Цей товар не належить вашому магазину" };
            }

            if (request.Status != PostSaleRequestStatus.AwaitingSellerConfirmation)
            {
                return new ServiceResult { Succeeded = false, Message = "Ця заявка не очікує підтвердження" };
            }

            request.Status = PostSaleRequestStatus.SellerDisputed;
            request.SellerDisputeReason = reason.Trim();
            request.UpdatedAt = DateTime.UtcNow;
            request.StatusChangedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = "Вашу відмову передано адміністратору на розгляд.", EntityId = request.Id };
        }
        public async Task<ServiceResult> SellerConfirmReturnAsync(Guid requestId, Guid businessId)
        {
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

                if (request.Status != PostSaleRequestStatus.AwaitingSellerConfirmation)
                {
                    return new ServiceResult { Succeeded = false, Message = "Ця заявка не очікує підтвердження" };
                }

                var business = request.OrderItem.Product.Business!;
                var amount = Math.Round(request.OrderItem.UnitPrice * request.OrderItem.Quantity, 2, MidpointRounding.AwayFromZero);
                var netAmount = amount - request.OrderItem.CommissionAmount;

                if (business.Balance < netAmount)
                {
                    await transaction.RollbackAsync();
                    return new ServiceResult
                    {
                        Succeeded = false,
                        Message = $"Недостатньо коштів на балансі магазину (потрібно {netAmount:N2} ₴). Поповніть баланс і спробуйте ще раз до {request.SellerConfirmDeadline:dd.MM.yyyy HH:mm}."
                    };
                }

                var paymentRefunded = await _paymentService.RefundPaymentAsync(request.OrderId.ToString(), amount);
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
                request.ResolvedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;
                request.StatusChangedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var scheduler = await _schedulerFactory.GetScheduler();
                await scheduler.DeleteJob(new JobKey($"postsale-deadline-job-{request.Id}", "postsale"));

                await _notificationPusher.PushToUserAsync(request.BuyerId,
                    "Продавець підтвердив повернення — кошти вже повернуто.", "ReturnStatus");

                return new ServiceResult { Succeeded = true, Message = "Повернення підтверджено, кошти повернуто покупцю.", EntityId = request.Id };

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PostSaleRequestDto>> GetAllOpenRequestsAsync()
        {
            var requests = await _context.PostSaleRequests
                .Include(r => r.Buyer)
                .Include(r => r.Images)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .Where(r => r.Status == PostSaleRequestStatus.Pending || r.Status == PostSaleRequestStatus.SellerDisputed)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(ToDto).ToList();
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
            ResolvedAt = r.ResolvedAt,
            SellerConfirmDeadline = r.SellerConfirmDeadline,
            SellerDisputeReason = r.SellerDisputeReason
        };
    }
}