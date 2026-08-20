using Microsoft.AspNetCore.Mvc.Rendering;
using Marketplace.BLL.DTOs.Products;

namespace Marketplace.Web.Models
{
    // Додаємо ": CatalogIndexViewModel"
    public class CatalogPageViewModel : CatalogIndexViewModel
    {
        public CatalogFilterViewModel Filter { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<CatalogProductViewModel> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        // Проксі-властивості для сумісності з Razor-поданнями
        public string? Search
        {
            get => Filter?.Search;
            set { Filter ??= new CatalogFilterViewModel(); Filter.Search = value; }
        }

        public Guid? CategoryId
        {
            get => Filter?.CategoryId;
            set { Filter ??= new CatalogFilterViewModel(); Filter.CategoryId = value; }
        }

        public string? SortBy
        {
            get => Filter?.Sort.ToString();
            set
            {
                Filter ??= new CatalogFilterViewModel();
                if (Enum.TryParse<CatalogSortOption>(value, true, out var parsedSort))
                {
                    Filter.Sort = parsedSort;
                }
            }
        }
    }
}