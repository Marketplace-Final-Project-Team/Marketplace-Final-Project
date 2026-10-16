using Marketplace.BLL.Interfaces;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Marketplace.BLL.Jobs
{
    public class ProductTranslationJob : IJob
    {
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IProductTranslationService _translationService;
        private readonly ILogger<ProductTranslationJob> _logger;

        public ProductTranslationJob(
            IGenericRepository<Product> productRepository,
            IProductTranslationService translationService,
            ILogger<ProductTranslationJob> logger)
        {
            _productRepository = productRepository;
            _translationService = translationService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var productId = context.JobDetail.JobDataMap.GetGuidValue("ProductId");

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Продукт {ProductId} не знайдено для перекладу", productId);
                return;
            }

            await _translationService.TranslateAndSaveAsync(product);

            _logger.LogInformation("Продукт {ProductId} перекладено на en/de/fr/es", productId);
        }
    }
}
