using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.DAL.Interfaces;
using Marketplace.Domain.Enums;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class BusinessModerationJob: IJob
    {
        private readonly IBusinessRepository _businessRepository;
        private readonly IContentModerationService _moderationService;
        private readonly AppDbContext _context;
        private readonly ILogger<BusinessModerationJob> _logger;

        public BusinessModerationJob(
            IBusinessRepository businessRepository,
            IContentModerationService moderationService,
            AppDbContext context,
            ILogger<BusinessModerationJob> logger)
        {
            _businessRepository = businessRepository;
            _moderationService = moderationService;
            _context = context;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var businessId = context.JobDetail.JobDataMap.GetGuidValue("BusinessId");

            var business = await _businessRepository.GetByIdAsync(businessId);
            if (business == null)
            {
                _logger.LogWarning("Бізнес {BusinessId} не знайдено для модерації", businessId);
                return;
            }

            try
            {
                var result = await _moderationService.CheckBusinessAsync(business);
                business.Status = result.IsApproved ? BusinessStatus.Active : BusinessStatus.Rejected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка модерації бізнесу {BusinessId}", businessId);
                business.Status = BusinessStatus.Pending;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Бізнес {BusinessId} промодеровано зі статусом {Status}", businessId, business.Status);
        }
    }
}
