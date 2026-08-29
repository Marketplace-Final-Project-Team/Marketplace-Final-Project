using Marketplace.BLL.Common;
using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class AdminProductsPageViewModel
    {
        public PagedResult<ProductItemAdminViewModel> Result { get; set; } = new();
        public ProductAdminFilterViewModel Filter { get; set; } = new();
        public IReadOnlyList<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }
}
