using ClosedXML.Excel;
using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Quartz;
using AdultContentStatus = Marketplace.Domain.Entities.AdultContentStatus;

namespace Marketplace.BLL.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<ProductImage> _productImageRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly AppDbContext _context;
    private readonly ISchedulerFactory _schedulerFactory;

    public ProductService(
        IGenericRepository<Product> productRepository,
        IGenericRepository<ProductImage> productImageRepository,
        IImageStorageService imageStorageService,
        AppDbContext context,
        ISchedulerFactory schedulerFactory)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;   
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

    private async Task<List<string>> UploadManyAsync(IEnumerable<IFormFile>? files)
    {
        if (files == null) return new List<string>();

        var list = files.ToList();
        var urls = await Task.WhenAll(list.Select(f => _imageStorageService.UploadImageAsync(f)));

        var failed = list.Where((_, i) => urls[i] == null).Select(f => f.FileName).ToList();
        if (failed.Count > 0) throw new InvalidOperationException($"Не вдалося завантажити зображення: {string.Join(", ", failed)}.");
       
        return urls.Select(u => u!).ToList();
    }

    public async Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null, bool allowAdultContent = false)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Discounts)
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

        if (dto.MainImage == null)
            throw new InvalidOperationException("Головне зображення товару обов'язкове.");

        var variants = BuildVariants(dto.Variants);

        var mainImageUrl = await _imageStorageService.UploadImageAsync(dto.MainImage);
        if (mainImageUrl == null)
            throw new InvalidOperationException("Не вдалося завантажити головне зображення.");

        var additionalImageUrls = await UploadManyAsync(dto.AdditionalImages);

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
            CreatedAt = DateTime.UtcNow,
            MainImageUrl = mainImageUrl!,
        };

        foreach (var url in additionalImageUrls)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = url
            });
        }

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
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product == null) return null;

        string? newMainUrl = null;
        if (dto.MainImage != null)
        {
            newMainUrl = await _imageStorageService.UploadImageAsync(dto.MainImage);
            if (newMainUrl == null) throw new InvalidOperationException("Не вдалося завантажити головне зображення.");
        }

        var newAdditionalUrls = await UploadManyAsync(dto.AdditionalImages);

        var keepUrls = (dto.ExistingAdditionalImageUrls ?? new List<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toRemove = product.Images
            .Where(img => !keepUrls.Contains(img.ImageUrl))
            .ToList();

        _context.RemoveRange(toRemove);

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim();
        product.IsAdultOnly = dto.IsAdultOnly;
        product.Status = ProductStatus.Pending;
        product.ModerationReason = null;

        if (newMainUrl != null) product.MainImageUrl = newMainUrl;

        foreach (var url in newAdditionalUrls)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = url
            });
        }

        SyncVariants(product, dto.Variants);

        var activeVariants = product.Variants.Where(v => !v.IsDeleted).ToList();
        product.Quantity = activeVariants.Count > 0
            ? activeVariants.Sum(v => v.Quantity)
            : dto.Quantity;

        await _context.SaveChangesAsync();

        await ScheduleModerationJobAsync(product.Id);
        await ScheduleTranslationJobAsync(product.Id);

        return product;
    }

    public async Task<bool> DeleteAsync(Guid id)
        => await DeleteOrArchiveAsync(id) != ProductDeleteOutcome.NotFound;

    public async Task<ProductDeleteOutcome> DeleteOrArchiveAsync(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return ProductDeleteOutcome.NotFound;

        var hasOrders = await _context.OrderItems
            .IgnoreQueryFilters()
            .AnyAsync(oi => oi.ProductId == id);

        if (hasOrders)
        {
            if (product.Status != ProductStatus.Archived)
            {
                product.Status = ProductStatus.Archived;
                product.ModerationReason = null;
                await _context.SaveChangesAsync();
            }

            return ProductDeleteOutcome.Archived;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return ProductDeleteOutcome.Deleted;
    }

    public async Task<(int OpenOrders, bool HasOrders)> GetOrderUsageAsync(Guid productId)
    {
        var hasOrders = await _context.OrderItems
            .IgnoreQueryFilters()
            .AnyAsync(oi => oi.ProductId == productId);

        var openOrders = await _context.OrderItems
            .Where(oi => oi.ProductId == productId
                && !oi.IsReturned
                && (oi.Order.Status == OrderStatus.Pending
                    || oi.Order.Status == OrderStatus.Confirmed
                    || oi.Order.Status == OrderStatus.Shipped))
            .Select(oi => oi.OrderId)
            .Distinct()
            .CountAsync();

        return (openOrders, hasOrders);
    }


    public async Task<ServiceResult> UpdateQuantityAsync(Guid productId, int quantity)
    {
        if (quantity < 0)
        {
            return new ServiceResult { Succeeded = false, Message = "Кількість не може бути від'ємною." };
        }

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return new ServiceResult { Succeeded = false, Message = "Товар не знайдено." };
        }

        if (product.Variants.Count > 0)
        {
            return new ServiceResult { Succeeded = false, Message = "Товар має розміри - кількість редагується через сторінку товару." };
        }

        product.Quantity = quantity;
        await _context.SaveChangesAsync();

        return new ServiceResult { Succeeded = true, Message = string.Empty };
    }

    public async Task<IReadOnlyList<ReservedProductDto>> GetReservedProductsAsync(Guid businessId)
    {
        return await _context.OrderItems
            .Where(oi => oi.Product.BusinessId == businessId
                && !oi.IsReturned
                && (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.Confirmed))
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.Sku, oi.Product.MainImageUrl, oi.Product.Quantity })
            .Select(g => new ReservedProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                Sku = g.Key.Sku,
                MainImageUrl = g.Key.MainImageUrl,
                ReservedQuantity = g.Sum(x => x.Quantity),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count(),
                TotalQuantity = g.Key.Quantity
            })
            .OrderByDescending(r => r.ReservedQuantity)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<StockForecastDto>> GetStockForecastAsync(Guid businessId, int lookbackDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-lookbackDays);

        var salesByProduct = await _context.OrderItems
            .Where(oi => oi.Product.BusinessId == businessId
                && oi.Order.CreatedAt >= cutoff
                && oi.Order.Status != OrderStatus.Canceled
                && !oi.IsReturned)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Sold);

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.BusinessId == businessId && p.Status == ProductStatus.Active)
            .ToListAsync();

        return products
            .Select(p => new StockForecastDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Sku = p.Sku,
                MainImageUrl = p.MainImageUrl,
                CurrentQuantity = p.Quantity,
                UnitsSoldLast30Days = salesByProduct.TryGetValue(p.Id, out var sold) ? sold : 0
            })
            .Where(f => f.UnitsSoldLast30Days > 0)
            .OrderBy(f => f.EstimatedDaysRemaining ?? int.MaxValue)
            .ToList();
    }

    private static readonly string[] ExcelHeaders =
    {
        "Артикул (SKU)", "Назва", "Опис", "Категорія", "Ціна", "Кількість", "Статус"
    };

    public async Task<byte[]> ExportToExcelAsync(Guid businessId)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.BusinessId == businessId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Товари");

        for (var col = 0; col < ExcelHeaders.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = ExcelHeaders[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var product in products)
        {
            sheet.Cell(row, 1).Value = product.Sku ?? string.Empty;
            sheet.Cell(row, 2).Value = product.Name;
            sheet.Cell(row, 3).Value = product.Description;
            sheet.Cell(row, 4).Value = product.Category?.Name ?? string.Empty;
            sheet.Cell(row, 5).Value = product.Price;
            sheet.Cell(row, 6).Value = product.Quantity;
            sheet.Cell(row, 7).Value = product.Status.ToString();
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GetImportExampleAsync()
    {
        var sampleCategories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => c.Name)
            .Take(2)
            .ToListAsync();

        var categoryA = sampleCategories.ElementAtOrDefault(0) ?? "Електроніка";
        var categoryB = sampleCategories.ElementAtOrDefault(1) ?? "Одяг";

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Товари");

        for (var col = 0; col < ExcelHeaders.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = ExcelHeaders[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var exampleRows = new (string Sku, string Name, string Description, string Category, decimal Price, int Quantity)[]
        {
            ("", "Бездротові навушники Sound Pro", "Навушники з активним шумозаглушенням і зарядом до 30 годин.", categoryA, 1499.00m, 25),
            ("SKU-00231", "Джинсова куртка", "Класична джинсова куртка унісекс, розмір вказується під час створення товару.", categoryB, 1450.00m, 12)
        };

        var row = 2;
        foreach (var example in exampleRows)
        {
            sheet.Cell(row, 1).Value = example.Sku;
            sheet.Cell(row, 2).Value = example.Name;
            sheet.Cell(row, 3).Value = example.Description;
            sheet.Cell(row, 4).Value = example.Category;
            sheet.Cell(row, 5).Value = example.Price;
            sheet.Cell(row, 6).Value = example.Quantity;
            sheet.Cell(row, 7).Value = string.Empty;
            row++;
        }

        var noteRow = row + 1;
        sheet.Cell(noteRow, 1).Value = "Підказки:";
        sheet.Cell(noteRow, 1).Style.Font.Bold = true;
        sheet.Cell(noteRow + 1, 1).Value = "- Порожній «Артикул (SKU)» = новий товар буде створено.";
        sheet.Cell(noteRow + 2, 1).Value = "- Якщо «Артикул (SKU)» збігається з наявним товаром - товар оновиться.";
        sheet.Cell(noteRow + 3, 1).Value = "- «Категорія» має точно збігатися з назвою наявної категорії каталогу.";
        sheet.Cell(noteRow + 4, 1).Value = "- «Статус» не імпортується - будь-який змінений товар повторно йде на модерацію.";
        sheet.Cell(noteRow + 5, 1).Value = "- Товари з розмірами (одяг/взуття) через імпорт не оновлюються - редагуйте їх вручну.";

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ProductImportResultDto> ImportFromExcelAsync(Guid businessId, Stream fileStream)
    {
        var result = new ProductImportResultDto();

        var categories = await _context.Categories.AsNoTracking().ToListAsync();
        var existingProducts = await _context.Products
            .Where(p => p.BusinessId == businessId)
            .ToListAsync();
        var touchedProductIds = new List<Guid>();

        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheet(1);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = sheet.Row(rowNumber);
            var sku = row.Cell(1).GetString().Trim();
            var name = row.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(sku))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                result.RowErrors.Add(new ProductImportRowError { RowNumber = rowNumber, ProductName = sku, Error = "Не вказано назву товару." });
                continue;
            }

            var description = row.Cell(3).GetString().Trim();
            var categoryName = row.Cell(4).GetString().Trim();
            var category = categories.FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            if (category is null)
            {
                result.RowErrors.Add(new ProductImportRowError { RowNumber = rowNumber, ProductName = name, Error = $"Категорію «{categoryName}» не знайдено." });
                continue;
            }

            if (!row.Cell(5).TryGetValue(out decimal price) || price <= 0)
            {
                result.RowErrors.Add(new ProductImportRowError { RowNumber = rowNumber, ProductName = name, Error = "Некоректна ціна." });
                continue;
            }

            if (!row.Cell(6).TryGetValue(out int quantity) || quantity < 0)
            {
                result.RowErrors.Add(new ProductImportRowError { RowNumber = rowNumber, ProductName = name, Error = "Некоректна кількість." });
                continue;
            }

            var existing = string.IsNullOrWhiteSpace(sku)
                ? null
                : existingProducts.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                if (existing.Variants.Count > 0)
                {
                    result.RowErrors.Add(new ProductImportRowError
                    {
                        RowNumber = rowNumber,
                        ProductName = name,
                        Error = "Товар має розміри - оновлення через імпорт недоступне. Редагуйте його вручну."
                    });
                    continue;
                }

                existing.Name = name;
                existing.Description = description;
                existing.CategoryId = category.Id;
                existing.Price = price;
                existing.Quantity = quantity;
                existing.Status = ProductStatus.Pending;
                existing.ModerationReason = null;

                touchedProductIds.Add(existing.Id);
                result.UpdatedCount++;
            }
            else
            {
                var created = new Product
                {
                    Id = Guid.NewGuid(),
                    BusinessId = businessId,
                    CategoryId = category.Id,
                    Name = name,
                    Description = description,
                    Price = price,
                    Quantity = quantity,
                    Sku = string.IsNullOrWhiteSpace(sku) ? null : sku,
                    Status = ProductStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _productRepository.AddAsync(created);
                existingProducts.Add(created);

                touchedProductIds.Add(created.Id);
                result.CreatedCount++;
            }
        }

        await _context.SaveChangesAsync();

        foreach (var productId in touchedProductIds)
        {
            await ScheduleModerationJobAsync(productId);
            await ScheduleTranslationJobAsync(productId);
        }

        return result;
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