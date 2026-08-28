using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class ProductModerationJob: IJob
    {
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IContentModerationService _moderationService;
        private readonly AppDbContext _context;
        private readonly ILogger<ProductModerationJob> _logger;

        public ProductModerationJob(
            IGenericRepository<Product> productRepository,
            IContentModerationService moderationService,
            AppDbContext context,
            ILogger<ProductModerationJob> logger)
        {
            _productRepository = productRepository;
            _moderationService = moderationService;
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка модерації продукту {ProductId}", productId);
                product.Status = ProductStatus.Pending;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Продукт {ProductId} промодеровано зі статусом {Status}", productId, product.Status);
        }
    }
}
