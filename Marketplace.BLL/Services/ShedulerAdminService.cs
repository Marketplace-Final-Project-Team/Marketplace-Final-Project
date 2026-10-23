using Google.Api.Gax;
using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;
using Quartz;
using Quartz.Impl.Matchers;

namespace Marketplace.BLL.Services
{
    public class ShedulerAdminService : IShedulerAdminService
    {
        private ISchedulerFactory _schedulerFactory;
        private readonly IProductService _productService;
        private readonly IBusinessService _businessService;

        public ShedulerAdminService(
            ISchedulerFactory schedulerFactory,
            IProductService productService,
            IBusinessService businessService
            )
        {
            _schedulerFactory = schedulerFactory;
            _productService = productService;
            _businessService = businessService;
        }

        public async Task<bool> PauseAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            if (scheduler.InStandbyMode)
            {
                return false;
            }

            await scheduler.Standby();
            return true;
        }

        public async Task<bool> ResumeAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            if (!scheduler.InStandbyMode)
            {
                return false;
            }

            await scheduler.Start();
            return true;
        }

        public async Task<bool> TriggerModerationJobAsync(Guid entityId, string entityType)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobName = entityType == "Business" ? $"business-moderation-job-{entityId}" : $"product-moderation-job-{entityId}";
            var jobKey = new JobKey(jobName, "moderation");

            if (!await scheduler.CheckExists(jobKey))
            {
                return false;
            }

            await scheduler.TriggerJob(jobKey);
            return true;
        }

        public async Task<bool> TriggerRecurringJobAsync(string groupName, string jobName)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKey = new JobKey(jobName, groupName);

            if (!await scheduler.CheckExists(jobKey))
            {
                return false;
            }

            await scheduler.TriggerJob(jobKey);
            return true;
        }

        public async Task<IReadOnlyList<SheduledModerationJobDto>> GetSheduledJobsAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
         
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals("moderation"));
            var sheduledJobs = new List<SheduledModerationJobDto>();

            foreach (var jobKey in jobKeys)
            {
                string? entityType = null;
                Guid entityId;

                if (jobKey.Name.StartsWith("product-moderation-job-") &&
                    Guid.TryParse(jobKey.Name.Replace("product-moderation-job-", string.Empty), out var productId))
                {
                    entityType = "Product";
                    entityId = productId;
                }
                else if (jobKey.Name.StartsWith("business-moderation-job-") &&
                    Guid.TryParse(jobKey.Name.Replace("business-moderation-job-", string.Empty), out var businessId))
                {
                    entityType = "Business";
                    entityId = businessId;
                }
                else
                {
                    continue;
                }

                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                var nextFireTime = triggers
                    .Select(t => t.GetNextFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderBy(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                string? name = entityType == "Product"
                    ? (await _productService.GetByIdAsync(entityId))?.Name
                    : (await _businessService.GetByIdAsync(entityId))?.StoreName;

                sheduledJobs.Add(new SheduledModerationJobDto
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityName = name,
                    NextRunAtUtc = nextFireTime
                });
            }

            return sheduledJobs;
        }

        public async Task<IReadOnlyList<RecurringJobDto>> GetReccuringJobsAsync()
        {
            var sheduler = await _schedulerFactory.GetScheduler();

            var recurringJobs = new List<RecurringJobDto>();
            var cleanupJobKey = new JobKey("pending-order-expiry-job", "cleanup");
            
            if (await sheduler.CheckExists(cleanupJobKey))
            {
                var cleanupTriggers = await sheduler.GetTriggersOfJob(cleanupJobKey);
                var nextRun = cleanupTriggers
                    .Select(t => t.GetNextFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderBy(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();
                var previousRun = cleanupTriggers
                    .Select(t => t.GetPreviousFireTimeUtc())
                    .Where(t => t.HasValue)
                    .Select(t => t!.Value.UtcDateTime)
                    .OrderByDescending(t => t)
                    .Cast<DateTime?>()
                    .FirstOrDefault();

                recurringJobs.Add(new RecurringJobDto
                {
                    GroupName = cleanupJobKey.Group,
                    JobName = cleanupJobKey.Name,
                    DisplayName = "Автоскасування прострочених замовлень",
                    Description = "Щогодини скасовує замовлення зі статусом «Очікує підтвердження», якщо оплата не надійшла протягом налаштованого часу (за замовчуванням 48 год.).",
                    NextRunAtUtc = nextRun,
                    PreviousRunAtUtc = previousRun
                });
            }

            return recurringJobs;
        }

        public async Task<bool> IsRunningAsync()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            return scheduler.IsStarted && !scheduler.InStandbyMode && !scheduler.IsShutdown;
        }
    }
}
