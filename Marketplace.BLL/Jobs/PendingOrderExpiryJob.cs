using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Jobs
{
    public class PendingOrderExpiryJob : IJob
    {
        public const string ActivityLogAction = "OrderAutoExpired";

        private readonly AppDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<PendingOrderExpiryJob> _logger;

        public PendingOrderExpiryJob(
            AppDbContext context,
            IOrderService orderService,
            ISystemSettingsService settingsService,
            IActivityLogService activityLog,
            ILogger<PendingOrderExpiryJob> logger)
        {
            _context = context;
            _orderService = orderService;
            _settingsService = settingsService;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var expiryHours = await _settingsService.GetIntAsync("BusinessRules", "PendingOrderExpiryHours", 48);
            var cutoff = DateTime.UtcNow.AddHours(-expiryHours);

            var expiredOrders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
                .ToListAsync();

            foreach (var order in expiredOrders)
            {
                var businessIds = order.Items
                    .Where(i => i.Product != null)
                    .Select(i => i.Product.BusinessId)
                    .Distinct()
                    .ToList();

                var result = await _orderService.ChangeOrderStatus(order.Id, OrderStatus.Canceled);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Не вдалося автоматично скасувати прострочене замовлення {OrderId}: {Message}", order.Id, result.Message);
                    continue;
                }

                foreach (var businessId in businessIds)
                {
                    await _activityLog.LogBusinessActionAsync(businessId, ActivityLogAction,
                        $"Замовлення #{order.Id} автоматично скасовано - очікування оплати перевищило {expiryHours} год.",
                        relatedEntityId: order.Id);
                }

                _logger.LogInformation("Замовлення {OrderId} автоматично скасовано через {Hours} год. очікування оплати", order.Id, expiryHours);
            }
        }
    }
}
