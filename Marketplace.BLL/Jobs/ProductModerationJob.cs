using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Jobs
{
    public class ProductModerationJob: IJob
    {
        public const string ActivityLogAction = "ProductAutoModerated";

        private readonly IGenericRepository<Product> _productRepository;
        private readonly IContentModerationService _moderationService;
        private readonly IActivityLogService _activityLog;
        private readonly AppDbContext _context;
        private readonly ILogger<ProductModerationJob> _logger;

        public ProductModerationJob(
            IGenericRepository<Product> productRepository,
            IContentModerationService moderationService,
            IActivityLogService activityLog,
            AppDbContext context,
            ILogger<ProductModerationJob> logger)
        {
            _productRepository = productRepository;
            _moderationService = moderationService;
            _activityLog = activityLog;
            _context = context;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var productId = context.JobDetail.JobDataMap.GetGuidValue("ProductId");

            var product = await _productRepository.GetByIdAsync(productId);
            if(product == null)
            {
                _logger.LogWarning("Продукт {ProductId} не знайдено для модерації", productId);
                return;
            }

            try
            {
                var result = await _moderationService.CheckProductContentAsync(product);
                product.Status = result.IsApproved ? ProductStatus.Active : ProductStatus.Banned;
                product.ModerationReason = result.IsApproved ? null : result.Reason;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка модерації продукту {ProductId}", productId);
                product.Status = ProductStatus.Pending;
                product.ModerationReason = "Не вдалося автоматично перевірити товар - очікує на ручну модерацію.";
            }

            await _context.SaveChangesAsync();

            await _activityLog.LogBusinessActionAsync(product.BusinessId, ActivityLogAction,
                $"Товар «{product.Name}» автоматично промодеровано зі статусом «{product.Status}»" +
                (string.IsNullOrWhiteSpace(product.ModerationReason) ? "." : $": {product.ModerationReason}"),
                relatedEntityId: product.Id);

            _logger.LogInformation("Продукт {ProductId} промодеровано зі статусом {Status}", productId, product.Status);
        }
    }
}
