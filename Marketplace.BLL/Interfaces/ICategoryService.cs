using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;
using Marketplace.Domain.Entities;
namespace Marketplace.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllCategories();
        Task<ServiceResult> CreateCategory(string categoryName, string? imageUrl = null);
        Task<ServiceResult> UpdateCategory(string newCategoryName, Guid categoryId, string? imageUrl = null);
        Task<ServiceResult> SetCategoryAdultStatus(Guid categoryId, bool isAdultOnly);
        Task<ServiceResult> DeleteCategory(Guid categoryId);

        Task<Category> GetByIdAsync(Guid categoryId);
    }
}
