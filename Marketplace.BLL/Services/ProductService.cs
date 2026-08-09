using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
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

        public async Task<IReadOnlyList<Product>> GetCatalogAsync(string? search = null)
        {
            var products = await _productRepository.FindAsync(
                product => product.Status == ProductStatus.Active && product.Quantity > 0);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                products = products
                    .Where(product =>
                        product.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                        || product.Description.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                        || product.Category.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return products
                .OrderByDescending(product => product.CreatedAt)
                .ToList();
        }

        public async Task<CatalogPageResultDto> GetCatalogPageAsync(CatalogFilterDto filter)
        {
            // Built directly against AppDbContext rather than IGenericRepository: filtering,
            // sorting by aggregated rating/sales and pagination all need query composition
            // (correlated subqueries, Skip/Take) that the generic repository's single-predicate
            // FindAsync cannot express. Product.Reviews is deliberately not used here — the
            // Review->Product relationship is configured with WithMany() (no back-reference) in
            // AppDbContext, so that navigation is backed by an unused shadow FK and is always
            // empty; rating is computed from Reviews by ProductId instead.
            var query = _context.Products.Where(p => p.Status == ProductStatus.Active);

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
            return await _productRepository.FirstOrDefaultAsync(
                product => product.Id == id
                    && product.Status == ProductStatus.Active
                    && product.Quantity > 0);
        }

        public async Task<IReadOnlyList<Product>> GetByBusinessAsync(Guid businessId)
        {
            return await _productRepository.FindAsync(p => p.BusinessId == businessId);
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
    }
}
