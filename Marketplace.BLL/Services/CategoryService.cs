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
        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly AppDbContext _context;

        public CategoryService(IGenericRepository<Category> categoryService, AppDbContext context)
        {
            _categoryRepository = categoryService;
            _context = context;
        }

        public async Task<ServiceResult> CreateCategory(string categoryName)
        {
            if (await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == categoryName.ToLower()))
            {
                return new ServiceResult { Succeeded = false, Message = "Така категорія вже існує" };
            }

            await _categoryRepository.AddAsync(new Category { Name = categoryName });
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

            category.IsAdultOnly = isAdultOnly;
            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
        }

        public async Task<ServiceResult> UpdateCategory(string newCategoryName, Guid categoryId)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId);

            if (category == null)
            {
                return new ServiceResult { Succeeded = false, Message = "Категорії з таким айді не існує" };
            }

            if (await _categoryRepository.ExistsAsync(c => c.Name.ToLower() == newCategoryName.ToLower() && c.Id != categoryId))
            {
                return new ServiceResult { Succeeded = false, Message = "Така категорія вже існує" };
            }

            category.Name = newCategoryName;
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

            category.IsDeleted = true;
            _categoryRepository.Update(category);
            await _context.SaveChangesAsync();

            return new ServiceResult { Succeeded = true, Message = string.Empty };
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
