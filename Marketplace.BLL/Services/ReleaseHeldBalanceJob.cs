using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    // Щодня перевіряє всі ще "утримувані" позиції замовлень - якщо товар
    // доставлено і минуло 14+ днів (вікно повернень пройшло), знімає холд
    // із Business.HeldBalance, роблячи ці гроші реально доступними для
    // виведення.
    //
    // Позиції, по яких зараз іде розгляд повернення (відкрита заявка),
    // НЕ звільняються: гроші лишаються під холдом, доки заявку не вирішено.
    // Якщо повернення проведено - холд знімається в OrderService.RefundOrderItemAsync;
    // якщо заявку відхилено - задача звільнить гроші при наступному запуску.
    public class ReleaseHeldBalanceJob : IJob
    {
        public const string ActivityLogAction = "HeldBalanceReleased";
        private const int ReturnWindowDays = 14;

        private static readonly PostSaleRequestStatus[] OpenStatuses =
        {
            PostSaleRequestStatus.Pending,
            PostSaleRequestStatus.AwaitingSellerConfirmation,
            PostSaleRequestStatus.SellerDisputed
        };

        private readonly AppDbContext _context;
        private readonly ILogger<ReleaseHeldBalanceJob> _logger;

        public ReleaseHeldBalanceJob(AppDbContext context, ILogger<ReleaseHeldBalanceJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var cutoff = DateTime.UtcNow.Date.AddDays(-ReturnWindowDays);

            var itemsToRelease = await _context.OrderItems
                .Include(i => i.Order)
                .Include(i => i.Product)
                .Where(i => i.HeldAmount != null
                    && !i.IsBalanceReleased
                    && !i.IsReturned
                    && i.Order.Status == OrderStatus.Delivered
                    && (i.Order.UpdatedAt ?? i.Order.CreatedAt) <= cutoff
                    && !_context.PostSaleRequests.Any(r =>
                        r.OrderItemId == i.Id && OpenStatuses.Contains(r.Status)))
                .ToListAsync();

            if (itemsToRelease.Count == 0)
            {
                return;
            }

            var businessIds = itemsToRelease
                .Where(i => i.Product != null)
                .Select(i => i.Product!.BusinessId)
                .Distinct()
                .ToList();

            var businesses = await _context.Businesses
                .Where(b => businessIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b);

            foreach (var item in itemsToRelease)
            {
                if (item.Product is null || !businesses.TryGetValue(item.Product.BusinessId, out var business))
                {
                    continue;
                }

                business.HeldBalance -= item.HeldAmount!.Value;
                item.IsBalanceReleased = true;

                _logger.LogInformation(
                    "Звільнено холд {Amount:N2} ₴ для позиції {OrderItemId} (магазин {BusinessId}).",
                    item.HeldAmount.Value, item.Id, business.Id);
            }

            await _context.SaveChangesAsync();
        }
    }
}