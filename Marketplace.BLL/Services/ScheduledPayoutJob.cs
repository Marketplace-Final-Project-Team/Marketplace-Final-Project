using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class ScheduledPayoutJob : IJob
    {
        public const string ActivityLogAction = "ScheduledPayoutProcessed";

        private readonly AppDbContext _context;
        private readonly IWithdrawalService _withdrawalService;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<ScheduledPayoutJob> _logger;

        public ScheduledPayoutJob(
            AppDbContext context,
            IWithdrawalService withdrawalService,
            IActivityLogService activityLog,
            ILogger<ScheduledPayoutJob> logger)
        {
            _context = context;
            _withdrawalService = withdrawalService;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var today = DateTime.UtcNow.Date;
            var daysInCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var isLastDayOfMonth = today.Day == daysInCurrentMonth;

            var businesses = await _context.Businesses
                .Where(b => b.ScheduledPayoutEnabled
                    && (b.ScheduledPayoutDayOfMonth == today.Day
                        || (isLastDayOfMonth && b.ScheduledPayoutDayOfMonth > daysInCurrentMonth))
                    && b.ScheduledPayoutAmount != null
                    && b.ScheduledPayoutAmount > 0
                    && b.ScheduledPayoutCardId != null)
                .ToListAsync();

            foreach (var business in businesses)
            {
                if (business.LastScheduledPayoutAt?.Date == today)
                {
                    continue;
                }

                var amount = business.ScheduledPayoutAmount!.Value;

                var available = business.Balance - business.HeldBalance;

                if (available < amount)
                {
                    _logger.LogInformation(
                        "Заплановану виплату для магазину {BusinessId} пропущено - вільного балансу ({Available:N2}) не вистачає на заплановану суму ({Amount:N2}), {Held:N2} заброньовано під 14-денне вікно повернень.",
                        business.Id, available, amount, business.HeldBalance);

                    await _activityLog.LogBusinessActionAsync(business.Id, ActivityLogAction,
                        $"Заплановану автовиплату {amount:N2} ₴ пропущено - недостатньо вільного балансу ({available:N2} ₴ доступно, {business.HeldBalance:N2} ₴ заброньовано під можливі повернення).",
                        relatedEntityId: null);

                    continue;
                }

                var result = await _withdrawalService.Create(
                    business.Id, amount,
                    savedCardId: business.ScheduledPayoutCardId,
                    cardNumber: null, recipientName: null, rememberCard: false);

                business.LastScheduledPayoutAt = today;
                await _context.SaveChangesAsync();

                await _activityLog.LogBusinessActionAsync(business.Id, ActivityLogAction,
                    result.Succeeded
                        ? $"Заплановану автовиплату {amount:N2} ₴ оброблено: {result.Message}"
                        : $"Заплановану автовиплату не вдалося створити: {result.Message}",
                    relatedEntityId: null);

                _logger.LogInformation("Заплановану виплату для магазину {BusinessId} оброблено: {Message}", business.Id, result.Message);
            }
        }
    }
}