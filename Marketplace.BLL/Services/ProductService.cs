using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Quartz;
using AdultContentStatus = Marketplace.Domain.Entities.AdultContentStatus;

namespace Marketplace.BLL.Services;

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

        var jobKey = new JobKey($"product-moderation-job-{productId}", "moderation");

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
            .WithIdentity($"product-moderation-trigger-{productId}", "moderation")
            .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Second))
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    private async Task ScheduleTranslationJobAsync(Guid productId)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        var jobKey = new JobKey($"product-translation-job-{productId}", "translation");

        if (await scheduler.CheckExists(jobKey))
        {
            await scheduler.DeleteJob(jobKey);
        }

        var jobData = new JobDataMap
        {
            { "ProductId", productId }
        };

        var job = JobBuilder.Create<ProductTranslationJob>()
            .WithIdentity(jobKey)
            .UsingJobData(jobData)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"product-translation-trigger-{productId}", "translation")
            .StartAt(DateBuilder.FutureDate(7, IntervalUnit.Second))
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }

    public async Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null, bool allowAdultContent = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)  
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Include(p => p.Business)
                .ThenInclude(b => b.UserDiscounts)
            .Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active);

        if (!allowAdultContent)
        {
            query = query.Where(p => !p.IsAdultOnly);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(normalizedSearch)
                || p.Description.Contains(normalizedSearch)
                || (p.Category != null && p.Category.Name.Contains(normalizedSearch)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<CatalogPageResultDto> GetCatalogPageAsync(CatalogFilterDto filter, bool allowAdultContent = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)   
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Include(p => p.Business)
                .ThenInclude(b => b.UserDiscounts)
            .Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active);

        if (!allowAdultContent)
        {
            query = query.Where(p => !p.IsAdultOnly);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var normalizedSearch = filter.Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(normalizedSearch)
                || p.Description.Contains(normalizedSearch)
                || (p.Category != null && p.Category.Name.Contains(normalizedSearch)));
        }

        if (filter.InStockOnly)
        {
            query = query.Where(p => p.Quantity > 0);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        var totalCount = await query.CountAsync();
        var pageSize = filter.PageSize <= 0 ? 12 : filter.PageSize;
        var page = filter.Page <= 0 ? 1 : filter.Page;

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = products.Select(p => new CatalogPageItemDto
        {
            Product = p,
            AverageRating = 0,
            ReviewCount = 0
        }).ToList();

        return new CatalogPageResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Product?> GetCatalogProductAsync(Guid id, bool allowAdultContent = false)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)   
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Include(p => p.Business)
                .ThenInclude(b => b.UserDiscounts)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Active);

        if (product == null)
            return null;

        if (product.IsAdultOnly && !allowAdultContent)
            return null;

        return product;
    }

    public async Task<IReadOnlyList<CatalogProductDto>> GetSearchSuggestionsAsync(string? term, int limit = 8, bool allowAdultContent = false)
    {
        if (string.IsNullOrWhiteSpace(term))
            return new List<CatalogProductDto>();

        var normalized = term.Trim().ToLower();

        var productsQuery = _context.Products
            .AsNoTracking()
            
            .Include(p => p.Images)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)   
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Where(p => p.Status == ProductStatus.Active);

        if (!allowAdultContent)
        {
            productsQuery = productsQuery.Where(p => !p.IsAdultOnly);
        }

        return await productsQuery
            .Where(p => p.Name.ToLower().Contains(normalized))
            .Take(limit)
            .Select(p => new CatalogProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                MainImageUrl = p.Images
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId)
    {
        return await _context.Products
            
            .Include(p => p.Images)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)   
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Business)
                .ThenInclude(b => b.Discounts)   
            .Include(p => p.Category)
                .ThenInclude(c => c.Discounts)
            .Include(p => p.Business)
                .ThenInclude(b => b.UserDiscounts)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(ProductCreateDto dto)
    {
        if (dto.IsAdultOnly)
        {
            var business = await _context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == dto.BusinessId);
            if (business == null)
            {
                throw new InvalidOperationException("Магазин не знайдено.");
            }

            if (business.AdultStatus != AdultContentStatus.Approved)
            {
                throw new InvalidOperationException("Ваш магазин не має підтвердженого статусу для розміщення товарів 18+.");
            }
        }

        var variants = BuildVariants(dto.Variants);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = variants.Count > 0 ? variants.Sum(v => v.Quantity) : dto.Quantity,
            Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim(),
            IsAdultOnly = dto.IsAdultOnly,
            Status = ProductStatus.Pending,
            ModerationReason = null,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var variant in variants)
        {
            variant.ProductId = product.Id;
            product.Variants.Add(variant);
        }

        await _productRepository.AddAsync(product);
        await _context.SaveChangesAsync();

        await ScheduleModerationJobAsync(product.Id);
        await ScheduleTranslationJobAsync(product.Id);

        return product;
    }

    public async Task<Product?> UpdateAsync(ProductEditDto dto)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product == null)
            return null;

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim();
        product.IsAdultOnly = dto.IsAdultOnly;
        product.Status = ProductStatus.Pending;
        product.ModerationReason = null;

        SyncVariants(product, dto.Variants);
        product.Quantity = product.Variants.Count > 0 ? product.Variants.Sum(v => v.Quantity) : dto.Quantity;

        await _context.SaveChangesAsync();

        await ScheduleModerationJobAsync(product.Id);
        await ScheduleTranslationJobAsync(product.Id);

        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    private static List<ProductVariant> BuildVariants(IEnumerable<ProductVariantDto>? variants)
    {
        if (variants == null)
        {
            return new List<ProductVariant>();
        }

        return variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Size))
            .GroupBy(v => v.Size.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .Select(v => new ProductVariant
            {
                Size = v.Size.Trim(),
                Sku = v.Sku?.Trim() ?? string.Empty,
                Quantity = Math.Max(0, v.Quantity)
            })
            .ToList();
    }

    private static void SyncVariants(Product product, IEnumerable<ProductVariantDto>? variants)
    {
        var incoming = BuildVariants(variants);
        var existing = product.Variants.ToList();

        foreach (var variantDto in incoming)
        {
            var match = existing.FirstOrDefault(v => v.Size.Equals(variantDto.Size, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                match.Sku = variantDto.Sku;
                match.Quantity = variantDto.Quantity;
                existing.Remove(match);
            }
            else
            {
                variantDto.ProductId = product.Id;
                product.Variants.Add(variantDto);
            }
        }

        // Any size left in `existing` is no longer submitted by the seller — soft-delete it
        // rather than hard-remove, since it may already be referenced by past order history
        // (OrderItem -> ProductVariant is a Restrict FK, so a hard delete could fail there).
        foreach (var stale in existing)
        {
            stale.IsDeleted = true;
        }
    }
}