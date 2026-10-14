using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{

    public class PostSaleRequestModerationJob : IJob
    {
        public const string ActivityLogAction = "PostSaleRequestAiModerated";

        private readonly AppDbContext _context;
        private readonly IContentModerationService _moderationService;
        private readonly IPostSaleService _postSaleService;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<PostSaleRequestModerationJob> _logger;

        public PostSaleRequestModerationJob(
            AppDbContext context,
            IContentModerationService moderationService,
            IPostSaleService postSaleService,
            IActivityLogService activityLog,
            ILogger<PostSaleRequestModerationJob> logger)
        {
            _context = context;
            _moderationService = moderationService;
            _postSaleService = postSaleService;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var requestId = context.JobDetail.JobDataMap.GetGuidValue("RequestId");

            var request = await _context.PostSaleRequests
                .Include(r => r.Images)
                .Include(r => r.OrderItem)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(r => r.Id == requestId);


            if (request is null || request.Status != Domain.Enums.PostSaleRequestStatus.Pending)
            {
                _logger.LogInformation("Заявка {RequestId} вже не потребує AI-модерації (статус змінився).", requestId);
                return;
            }

            var result = await _moderationService.CheckReturnRequestAsync(request);

            if (!result.IsConfident)
            {
                _logger.LogInformation(
                    "AI-модерація заявки {RequestId}: ШІ не впевнений, лишаємо на ручний розгляд. Причина: {Reason}",
                    requestId, result.Reason);
                return;
            }

            if (result.ShouldApprove)
            {
                var approveResult = await _postSaleService.AdminApproveAsync(requestId);
                if (approveResult.Succeeded && request.OrderItem?.Product is not null)
                {
                    await _activityLog.LogBusinessActionAsync(
                        request.OrderItem.Product.BusinessId,
                        ActivityLogAction,
                        $"ШІ автоматично підтвердив заявку на повернення: {result.Reason}",
                        relatedEntityId: requestId);

                    _logger.LogInformation("Заявка {RequestId} автоматично схвалена ШІ.", requestId);
                }
            }
            else
            {
                var rejectResult = await _postSaleService.AdminRejectAsync(
                    requestId,
                    result.Reason ?? "Автоматично відхилено системою модерації.");

                if (rejectResult.Succeeded && request.OrderItem?.Product is not null)
                {
                    await _activityLog.LogBusinessActionAsync(
                        request.OrderItem.Product.BusinessId,
                        ActivityLogAction,
                        $"ШІ автоматично відхилив заявку на повернення: {result.Reason}",
                        relatedEntityId: requestId);

                    _logger.LogInformation("Заявка {RequestId} автоматично відхилена ШІ.", requestId);
                }
            }
        }
    }
}
