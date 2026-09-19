using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    // Запускається ОДНОРАЗОВО, точно в момент SellerConfirmDeadline (планується в
    // PostSaleService.AdminApproveAsync). Якщо продавець нічого не зробив за 7 днів:
    // 1. Пробуємо списати з балансу магазину (як при звичайному підтвердженні продавцем).
    // 2. Якщо коштів не вистачає — маркетплейс покриває повернення сам і призупиняє магазин.
    public class PostSaleAutoResolveJob : IJob
    {
        public const string ActivityLogAction = "PostSaleAutoResolved";

        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IMarketplaceBalanceService _marketplaceBalanceService;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<PostSaleAutoResolveJob> _logger;

        public PostSaleAutoResolveJob(
            AppDbContext context,
            IPaymentService paymentService,
            IOrderService orderService,
            IMarketplaceBalanceService marketplaceBalanceService,
            IActivityLogService activityLog,
            ILogger<PostSaleAutoResolveJob> logger)
        {
            _context = context;
            _paymentService = paymentService;
            _orderService = orderService;
            _marketplaceBalanceService = marketplaceBalanceService;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var requestId = context.JobDetail.JobDataMap.GetGuidValue("RequestId");

            var request = await _context.PostSaleRequests
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p!.Business)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            // Захисна перевірка — можливо, продавець уже підтвердив, або адмін вже
            // остаточно відхилив заявку раніше, ніж спрацював цей таймер. Тоді нічого не робимо.
            if (request is null || request.Status != PostSaleRequestStatus.AwaitingSellerConfirmation)
            {
                _logger.LogInformation("Заявка {RequestId} вже не потребує автообробки (статус змінився).", requestId);
                return;
            }

            var product = request.OrderItem.Product;
            var business = product?.Business;

            if (product is null || business is null)
            {
                _logger.LogWarning("Заявка {RequestId}: товар або магазин не знайдено, автообробка неможлива.", requestId);
                return;
            }

            var amount = Math.Round(request.OrderItem.UnitPrice * request.OrderItem.Quantity, 2, MidpointRounding.AwayFromZero);
            var netAmount = amount - request.OrderItem.CommissionAmount;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var paymentRefunded = await _paymentService.RefundPaymentAsync(request.OrderId.ToString(), amount);
                if (!paymentRefunded)
                {
                    _logger.LogError("Заявка {RequestId}: не вдалося повернути кошти через LiqPay при автообробці.", requestId);
                    await transaction.RollbackAsync();
                    return;
                }

                if (business.Balance >= netAmount)
                {
                    // На балансі продавця вистачає коштів — списуємо з нього, як завжди.
                    var stockResult = await _orderService.RefundOrderItemAsync(request.OrderItemId);
                    if (!stockResult.Succeeded)
                    {
                        _logger.LogError("Заявка {RequestId}: помилка синхронізації складу/балансу при автообробці.", requestId);
                        await transaction.RollbackAsync();
                        return;
                    }

                    await _activityLog.LogBusinessActionAsync(business.Id, ActivityLogAction,
                        $"Автоматичне повернення (продавець не підтвердив вчасно): {netAmount:N2} ₴ списано з балансу магазину.",
                        relatedEntityId: requestId);
                }
                else
                {
                    // Коштів на балансі магазину не вистачає — маркетплейс покриває повну суму сам
                    // і призупиняє роботу магазину до з'ясування обставин.
                    var marketplaceBalance = await _marketplaceBalanceService.GetOrCreateTrackedAsync();
                    marketplaceBalance.Balance -= amount;

                    product.Quantity += request.OrderItem.Quantity;
                    request.OrderItem.IsReturned = true;
                    business.Status = BusinessStatus.Suspended;

                    await _activityLog.LogBusinessActionAsync(business.Id, ActivityLogAction,
                        $"Магазин призупинено: недостатньо коштів для автоматичного повернення {amount:N2} ₴. Суму покрив маркетплейс.",
                        relatedEntityId: requestId);
                }

                request.Status = PostSaleRequestStatus.Refunded;
                request.ResolvedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Заявка {RequestId} автоматично оброблена, статус Refunded.", requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка автообробки заявки {RequestId}", requestId);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}