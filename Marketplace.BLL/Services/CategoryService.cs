using Marketplace.BLL.Common;
using Marketplace.BLL.Interfaces;
using Marketplace.DAL.Data;
using Marketplace.Domain.Entities;
using Marketplace.Domain.Enums;
using Marketplace.Domain.Interfaces;
using Marketplace.BLL.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.BLL.Services
{
    public class CategoryService : ICategoryService
    {
        
        public const string FallbackCategoryName = "Інше";

        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly AppDbContext _context;

        public CategoryService(IGenericRepository<Category> categoryService, AppDbContext context)
        {
            _categoryRepository = categoryService;
            _context = context;
        }

        private static bool IsFallback(string? name)
            => string.Equals(name?.Trim(), FallbackCategoryName, StringComparison.OrdinalIgnoreCase);

        public async Task<ServiceResult> CreateCategory(string categoryName, string? imageUrl = null)
        {
            if (await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == categoryName.ToLower()))
            {
                return new ServiceResult { Succeeded = false, Message = "Така категорія вже існує" };
            }

            await _categoryRepository.AddAsync(new Category
            {
                Name = categoryName,
                ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
            });
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<IReadOnlyList<CategoryDto>> GetAllCategories()
        {
            return await _categoryRepository.GetQueryable()
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl,
                    ActiveProductCount = c.Products.Count(p => !p.IsDeleted && p.Status == ProductStatus.Active),
                    IsAdultOnly = c.IsAdultOnly
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> SetCategoryAdultStatus(Guid categoryId, bool isAdultOnly)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);

            if (category == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Категорії з таким айді не існує" };
            }

           
            if (isAdultOnly && IsFallback(category.Name))
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = $"Категорію «{FallbackCategoryName}» не можна позначити як 18+: вона системна."
                };
            }

            category.IsAdultOnly = isAdultOnly;
            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> UpdateCategory(string newCategoryName, Guid categoryId, string? imageUrl = null)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);

            if (category == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Категорії з таким айді не існує" };
            }

           
            if (IsFallback(category.Name) && !IsFallback(newCategoryName))
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = $"Категорію «{FallbackCategoryName}» не можна перейменувати: вона системна."
                };
            }

            if (await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == newCategoryName.ToLower() && c.Id != categoryId))
            {
                return new ServiceResult { Succeeded = false, Message = "Така категорія вже існує" };
            }

            category.Name = IsFallback(category.Name) ? FallbackCategoryName : newCategoryName;
            category.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> DeleteCategory(Guid categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);

            if (category == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Категорії з таким айді не існує" };
            }

            if (IsFallback(category.Name))
            {
                return new ServiceResult
                {
                    Succeeded = false,
                    Message = $"Категорію «{FallbackCategoryName}» видалити не можна: до неї переносяться товари видалених категорій."
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

        
            var fallback = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == FallbackCategoryName && c.Id != categoryId);

            if (fallback == null)
            {
                fallback = new Category { Name = FallbackCategoryName, IsAdultOnly = false };
                _context.Categories.Add(fallback);
                await _context.SaveChangesAsync();
            }

            var fallbackId = fallback.Id;

            var movedProducts = await _context.Products
                .Where(p => p.CategoryId == categoryId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.CategoryId, fallbackId));

            await _context.Set<Discount>()
                .Where(d => d.CategoryId == categoryId)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsDeleted, true));

            category.IsDeleted = true;
            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new ServiceResult
            {
                Succeeded = true,
                Message = movedProducts > 0
                    ? $"Категорію видалено. Товарів перенесено в «{FallbackCategoryName}»: {movedProducts}."
                    : "Категорію видалено."
            };
        }

        public async Task<Category> GetByIdAsync(Guid categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category == null)
            {
                return null;
            }
            return category;
        }
    }
}