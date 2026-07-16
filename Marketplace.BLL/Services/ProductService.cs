using Marketplace.BLL.DTOs.Products;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;

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