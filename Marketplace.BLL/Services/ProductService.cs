using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services;

public class ProductService : IProductService
{
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly AppDbContext _context;

    public ProductService(
        IGenericRepository<Product> productRepository,
        IImageStorageService imageStorageService,
        AppDbContext context)
    {
        _productRepository = productRepository;
        _imageStorageService = imageStorageService;
        _context = context;
    }

    public async Task<PagedResult<CatalogProductDto>> GetCatalogAsync(ProductCatalogQueryDto query)
    {
        var now = DateTime.UtcNow;

        var itemsQuery = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active && p.Quantity > 0 && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.Trim().ToLower();
            itemsQuery = itemsQuery.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        if (query.CategoryId.HasValue)
        {
            itemsQuery = itemsQuery.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        itemsQuery = query.SortBy switch
        {
            "price_asc" => itemsQuery.OrderBy(p => p.Price),
            "price_desc" => itemsQuery.OrderByDescending(p => p.Price),
            _ => itemsQuery.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await itemsQuery.CountAsync();
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 12 : query.PageSize;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await itemsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.Quantity,
                MainImageUrl = p.MainImageUrl ?? string.Empty,
                CategoryName = p.Category != null ? p.Category.Name : null,
                ActiveDiscount = p.Discounts
                    .Where(d => d.StartDate <= now && d.EndDate >= now)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault()
            })
            .Select(x => new CatalogProductDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                MainImageUrl = x.MainImageUrl,
                Quantity = x.Quantity,
                CategoryName = x.CategoryName,
                HasActiveDiscount = x.ActiveDiscount != null,
                DiscountPercentage = x.ActiveDiscount != null ? x.ActiveDiscount.Percent : null,
                DiscountedPrice = x.ActiveDiscount != null
                    ? Math.Round(x.Price * (1 - (x.ActiveDiscount.Percent / 100m)), 2)
                    : null
            })
            .ToListAsync();

        return new PagedResult<CatalogProductDto>
        {
            Items = items,
            Page = page,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<Product?> GetCatalogProductAsync(Guid id)
    {
        return await _productRepository.FirstOrDefaultAsync(
            product => product.Id == id
                && product.Status == ProductStatus.Active
                && product.Quantity > 0
                && !product.IsDeleted);
    }

    public async Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId)
    {
        return await _productRepository.FindAsync(p => p.BusinessId == businessId && !p.IsDeleted);
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _productRepository.GetByIdAsync(id);
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

        var normalizedTerm = term.Trim().ToLower();
        var now = DateTime.UtcNow;

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active && p.Quantity > 0 && !p.IsDeleted)
            .Where(p => p.Business.Status == BusinessStatus.Active)
            .Where(p => p.Name.ToLower().Contains(normalizedTerm))
            .OrderBy(p => p.Name)
            .Take(limit)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                MainImageUrl = p.MainImageUrl ?? string.Empty,
                CategoryName = p.Category != null ? p.Category.Name : null,
                ActiveDiscount = p.Discounts
                    .Where(d => d.StartDate <= now && d.EndDate >= now)
                    .OrderByDescending(d => d.Percent)
                    .FirstOrDefault()
            })
            .Select(x => new CatalogProductDto
            {
                Id = x.Id,
                Name = x.Name,
                Price = x.Price,
                MainImageUrl = x.MainImageUrl,
                CategoryName = x.CategoryName,
                HasActiveDiscount = x.ActiveDiscount != null,
                DiscountPercentage = x.ActiveDiscount != null ? x.ActiveDiscount.Percent : null,
                DiscountedPrice = x.ActiveDiscount != null
                    ? Math.Round(x.Price * (1 - (x.ActiveDiscount.Percent / 100m)), 2)
                    : null
            })
            .ToListAsync();
    }
}