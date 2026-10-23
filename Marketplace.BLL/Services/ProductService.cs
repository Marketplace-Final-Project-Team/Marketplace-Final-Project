using ClosedXML.Excel;
using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.BLL.Jobs;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quartz;
using AdultContentStatus = Marketplace.Domain.Entities.AdultContentStatus;

namespace Marketplace.BLL.Services;

public class ProductService : IProductService
{
    private const int MaxAdditionalImageCount = 8;

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

    private async Task<string?> UploadImageAsync(Microsoft.AspNetCore.Http.IFormFile? image, string role)
    {
        if (image is null)
        {
            return null;
        }

        var imageUrl = await _imageStorageService.UploadImageAsync(image, "products");
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new InvalidOperationException($"Не вдалося завантажити {role} «{image.FileName}». Перевірте формат і розмір файлу та спробуйте ще раз.");
        }

        return imageUrl;
    }

    private async Task<List<string>> UploadAdditionalImagesAsync(
        IReadOnlyCollection<Microsoft.AspNetCore.Http.IFormFile>? images)
    {
        if (images is null || images.Count == 0)
        {
            return new List<string>();
        }

        if (images.Count > MaxAdditionalImageCount)
        {
            throw new InvalidOperationException($"Можна додати не більше {MaxAdditionalImageCount} додаткових зображень.");
        }

        var imageUrls = new List<string>(images.Count);
        foreach (var image in images)
        {
            imageUrls.Add((await UploadImageAsync(image, "додаткове фото"))!);
        }

        return imageUrls;
    }

    private async Task<(string Sku, bool WasGenerated)> ResolveSkuAsync(
        Guid businessId,
        string? requestedSku,
        Guid? excludedProductId = null)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(product => product.BusinessId == businessId);

        if (excludedProductId.HasValue)
        {
            query = query.Where(product => product.Id != excludedProductId.Value);
        }

        var existingSkus = await query
            .OrderByDescending(product => product.CreatedAt)
            .Select(product => product.Sku)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(requestedSku))
        {
            var normalizedSku = requestedSku.Trim();
            if (normalizedSku.Length > ProductSkuGenerator.MaxLength)
            {
                throw new InvalidOperationException($"Артикул не може містити більше {ProductSkuGenerator.MaxLength} символів.");
            }

            if (existingSkus.Contains(normalizedSku, StringComparer.OrdinalIgnoreCase))
            {
                throw new DuplicateProductSkuException(normalizedSku);
            }

            return (normalizedSku, false);
        }

        return (ProductSkuGenerator.Generate(existingSkus), true);
    }

    private async Task SaveChangesWithUniqueSkuRetryAsync(Product product, bool skuWasGenerated)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _context.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException exception) when (IsProductSkuUniqueConstraintViolation(exception))
            {
                if (!skuWasGenerated || attempt == maxAttempts)
                {
                    throw new DuplicateProductSkuException(product.Sku);
                }

                product.Sku = (await ResolveSkuAsync(product.BusinessId, null, product.Id)).Sku;
            }
        }
    }

    private static bool IsProductSkuUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains(
            "UX_Products_BusinessId_Sku",
            StringComparison.OrdinalIgnoreCase) == true;
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

    public async Task<CataloguePageResultDto> GetCataloguePageAsync(CatalogueFilterDto filter, bool allowAdultContent = false)
    {
        var query = _productRepository.GetQueryable()
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

        if (filter.InStockOnly == true)
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

        IOrderedQueryable<Product> ordered;

        switch (filter.Sort)
        {
            case CatalogueSortOption.PriceAsc:
                ordered = query.OrderBy(p => p.Price);
                break;

            case CatalogueSortOption.PriceDesc:
                ordered = query.OrderByDescending(p => p.Price);
                break;

            case CatalogueSortOption.Rating:
                ordered = query.OrderByDescending(p => p.Reviews
                    .Where(r => r.Status == ReviewStatus.Approved && !r.IsDeleted)
                    .Average(r => (double?)r.Rating) ?? 0);
                break;

            case CatalogueSortOption.Popularity:
                ordered = query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity));
                break;

            default:
                ordered = query.OrderByDescending(p => p.CreatedAt);
                break;
        }

        var products = await ordered
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = products.Select(p => new CataloguePageItemDto
        {
            Product = p,
            AverageRating = 0,
            ReviewCount = 0
        }).ToList();

        return new CataloguePageResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Product?> GetCatalogueProductAsync(Guid id, bool allowAdultContent = false)
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
            .AsNoTracking()
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

        var resolvedSku = await ResolveSkuAsync(dto.BusinessId, dto.Sku);
        var mainImageUrl = await UploadImageAsync(dto.MainImage, "головне фото");
        var additionalImageUrls = await UploadAdditionalImagesAsync(dto.AdditionalImages);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            BusinessId = dto.BusinessId,
            CategoryId = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = dto.Quantity,
            Sku = resolvedSku.Sku,
            MainImageUrl = mainImageUrl ?? string.Empty,
            IsAdultOnly = dto.IsAdultOnly,
            Status = ProductStatus.Pending,
            ModerationReason = null,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var imageUrl in additionalImageUrls)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = imageUrl
            });
        }

        await _productRepository.AddAsync(product);
        await SaveChangesWithUniqueSkuRetryAsync(product, resolvedSku.WasGenerated);

        await ScheduleModerationJobAsync(product.Id);
        await ScheduleTranslationJobAsync(product.Id);

        return product;
    }

    public async Task<Product?> UpdateAsync(ProductEditDto dto)
    {
        var product = await _context.Products
            .IgnoreAutoIncludes()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product == null) return null;

        var resolvedSku = await ResolveSkuAsync(product.BusinessId, dto.Sku, product.Id);
        var removedImageIds = dto.RemovedAdditionalImageIds.ToHashSet();
        var imagesToRemove = product.Images
            .Where(image => removedImageIds.Contains(image.Id))
            .ToList();
        var newAdditionalImageCount = dto.AdditionalImages?.Count ?? 0;
        var resultingAdditionalImageCount = product.Images.Count - imagesToRemove.Count + newAdditionalImageCount;

        if (resultingAdditionalImageCount > MaxAdditionalImageCount)
        {
            throw new InvalidOperationException($"У товару може бути не більше {MaxAdditionalImageCount} додаткових зображень. Видаліть зайві фото або виберіть менше нових.");
        }

        var mainImageUrl = await UploadImageAsync(dto.MainImage, "головне фото");
        var additionalImageUrls = await UploadAdditionalImagesAsync(dto.AdditionalImages);

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Sku = resolvedSku.Sku;
        if (mainImageUrl is not null)
        {
            product.MainImageUrl = mainImageUrl;
        }
        product.IsAdultOnly = dto.IsAdultOnly;
        product.Status = ProductStatus.Pending;
        product.ModerationReason = null;

        product.Quantity = dto.Quantity;

        if (imagesToRemove.Count > 0)
        {
            _context.ProductImages.RemoveRange(imagesToRemove);
        }

        foreach (var imageUrl in additionalImageUrls)
        {
            product.Images.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = imageUrl
            });
        }

        await SaveChangesWithUniqueSkuRetryAsync(product, resolvedSku.WasGenerated);

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
        sheet.Cell(noteRow + 1, 1).Value = "- Порожній «Артикул (SKU)» = новий товар буде створено з автоматично згенерованим артикулом.";
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

            if (sku.Length > ProductSkuGenerator.MaxLength)
            {
                result.RowErrors.Add(new ProductImportRowError
                {
                    RowNumber = rowNumber,
                    ProductName = name,
                    Error = $"Артикул не може містити більше {ProductSkuGenerator.MaxLength} символів."
                });
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
                    Sku = string.IsNullOrWhiteSpace(sku)
                        ? ProductSkuGenerator.Generate(existingProducts.Select(product => product.Sku))
                        : sku,
                    Status = ProductStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _productRepository.AddAsync(created);
                existingProducts.Add(created);

                touchedProductIds.Add(created.Id);
                result.CreatedCount++;
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsProductSkuUniqueConstraintViolation(exception))
        {
            throw new DuplicateProductSkuException("один з артикулів у файлі");
        }

        foreach (var productId in touchedProductIds)
        {
            await ScheduleModerationJobAsync(productId);
            await ScheduleTranslationJobAsync(productId);
        }

        return result;
    }
}
