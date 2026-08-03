using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.BLL.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryDto>> GetAllCategories();
        Task<ServiceResult> CreateCategory(string categoryName);
        Task<ServiceResult> UpdateCategory(string newCategoryName, Guid categoryId);
        Task<ServiceResult> DeleteCategory(Guid categoryId);
    }
}
