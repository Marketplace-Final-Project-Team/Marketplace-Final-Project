using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Marketplace.BLL.Services
{
    public class ProductService : IProductService
    {
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IImageStorageService _imageStorageService;
        private readonly AppDbContext _context;
        private readonly ISchedulerFactory _schedulerFactory;

        public ProductService(
            IGenericRepository<Product> productRepository,
            IImageStorageService imageStorageService,
            AppDbContext context,
            ISchedulerFactory schedulerFactory)
        {
            _productRepository = productRepository;
            _imageStorageService = imageStorageService;
            _context = context;
            _schedulerFactory = schedulerFactory;
        }

        private async Task ScheduleModerationJobAsync(Guid productId)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var jobKey = new JobKey($"moderation-job-{productId}", "moderation");

            if (await scheduler.CheckExists(jobKey))
            {
                await scheduler.DeleteJob(jobKey);
            }

            var jobData = new JobDataMap
            {
                { "ProductId", productId }
            };

            var job = JobBuilder.Create<ProductModerationJob>()
                .WithIdentity(jobKey)
                .UsingJobData(jobData)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"moderation-trigger-{productId}", "moderation")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }

        public async Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null)
        {
            var products = await _context.Products
                .Include(p => p.Discounts)
                .Include(p => p.Category)
                .Include(p => p.Business)
                    .ThenInclude(b => b.Discounts)
                .Where(product => product.Status == ProductStatus.Active && product.Quantity > 0)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                products = products
                    .Where(product =>
                        product.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                        || product.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                        || (product.Category != null && product.Category.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return products
                .OrderByDescending(product => product.CreatedAt)
                .ToList();
        }

        public async Task<CatalogPageResultDto> GetCatalogPageAsync(CatalogFilterDto filter)
        {
            var query = _context.Products
                .Include(p => p.Discounts)
                .Include(p => p.Category)
                .Include(p => p.Business)
                    .ThenInclude(b => b.Discounts)
                .Include(p => p.Images)
                .Where(p => p.Status == ProductStatus.Active);

            if (filter.InStockOnly)
            {
                query = query.Where(p => p.Quantity > 0);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var normalizedSearch = filter.Search.Trim();
                query = query.Where(p =>
                    p.Name.Contains(normalizedSearch)
                    || p.Description.Contains(normalizedSearch)
                    || p.Category.Name.Contains(normalizedSearch));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);
            }

            var projected = query.Select(p => new
            {
                Product = p,
                AverageRating = _context.Reviews
                    .Where(r => r.ProductId == p.Id)
                    .Average(r => (double?)r.Rating) ?? 0,
                ReviewCount = _context.Reviews.Count(r => r.ProductId == p.Id),
                SoldCount = _context.OrderItems
                    .Where(oi => oi.ProductId == p.Id && oi.Order.Status != OrderStatus.Canceled)
                    .Sum(oi => (int?)oi.Quantity) ?? 0
            });

            if (filter.MinRating.HasValue)
            {
                projected = projected.Where(x => x.AverageRating >= filter.MinRating.Value);
            }

            projected = filter.Sort switch
            {
                CatalogSortOption.PriceAsc => projected.OrderBy(x => x.Product.Price),
                CatalogSortOption.PriceDesc => projected.OrderByDescending(x => x.Product.Price),
                CatalogSortOption.Rating => projected.OrderByDescending(x => x.AverageRating).ThenByDescending(x => x.ReviewCount),
                CatalogSortOption.Popularity => projected.OrderByDescending(x => x.SoldCount),
                _ => projected.OrderByDescending(x => x.Product.CreatedAt)
            };

            var totalCount = await projected.CountAsync();

            var page = Math.Max(1, filter.Page);
            var pageSize = filter.PageSize <= 0 ? 12 : filter.PageSize;

            var pageItems = await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new CatalogPageResultDto
            {
                Items = pageItems
                    .Select(x => new CatalogPageItemDto
                    {
                        Product = x.Product,
                        AverageRating = Math.Round(x.AverageRating, 1),
                        ReviewCount = x.ReviewCount
                    })
                    .ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<Product?> GetCatalogProductAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Discounts)
                .Include(p => p.Category)
                .Include(p => p.Business)
                    .ThenInclude(b => b.Discounts)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(product => product.Id == id
                    && product.Status == ProductStatus.Active
                    && product.Quantity > 0);
        }

        public async Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId)
        {
            return await _productRepository.FindAsync(p => p.BusinessId == businessId);
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .Include(p => p.Discounts)
                .Include(p => p.Category)
                .Include(p => p.Business)
                    .ThenInclude(b => b.Discounts)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Product> CreateAsync(ProductCreateDto dto)
        {
            var product = new Product
            {
                BusinessId = dto.BusinessId,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Quantity = dto.Quantity,
                Status = ProductStatus.Pending
            };

            if (dto.MainImage != null)
            {
                var mainImageUrl = await _imageStorageService.UploadImageAsync(dto.MainImage);
                if (mainImageUrl != null)
                    product.MainImageUrl = mainImageUrl;
            }

            if (dto.AdditionalImages != null)
            {
                foreach (var file in dto.AdditionalImages)
                {
                    var url = await _imageStorageService.UploadImageAsync(file);
                    if (url != null)
                        product.Images.Add(new ProductImage { ImageUrl = url, ProductId = product.Id });
                }
            }

            await _productRepository.AddAsync(product);
            await _context.SaveChangesAsync();

            await ScheduleModerationJobAsync(product.Id);
            return product;
        }

        public async Task<Product?> UpdateAsync(ProductEditDto dto)
        {
            var product = await _productRepository.GetByIdAsync(dto.Id);
            if (product == null) return null;

            product.CategoryId = dto.CategoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Quantity = dto.Quantity;
            product.Status = ProductStatus.Pending;

            if (dto.MainImage != null)
            {
                var mainImageUrl = await _imageStorageService.UploadImageAsync(dto.MainImage);
                if (mainImageUrl != null)
                    product.MainImageUrl = mainImageUrl;
            }

            if (dto.AdditionalImages != null)
            {
                foreach (var file in dto.AdditionalImages)
                {
                    var url = await _imageStorageService.UploadImageAsync(file);
                    if (url != null)
                        product.Images.Add(new ProductImage { ImageUrl = url, ProductId = product.Id });
                }
            }

            _productRepository.Update(product);
            await _context.SaveChangesAsync();

            await ScheduleModerationJobAsync(product.Id);
            return product;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return false;

            product.IsDeleted = true;
            _productRepository.Update(product);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IReadOnlyList<CatalogProductDto>> GetSearchSuggestionsAsync(string? term, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
                return Array.Empty<CatalogProductDto>();

            var normalizedTerm = term.Trim();
            var now = DateTime.UtcNow;

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Discounts)
                .Where(p => p.Status == ProductStatus.Active && p.Quantity > 0)
                .Where(p => p.Name.Contains(normalizedTerm))
                .OrderBy(p => p.Name)
                .Take(limit)
                .ToListAsync();

            return products.Select(p =>
            {
                var activeDiscount = p.Discounts?
                    .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault();

                var finalPrice = activeDiscount != null && activeDiscount.Percent > 0
                    ? p.Price * (1 - (decimal)activeDiscount.Percent / 100)
                    : p.Price;

                return new CatalogProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = finalPrice,
                    MainImageUrl = p.MainImageUrl
                };
            }).ToList();
        }
    }
}