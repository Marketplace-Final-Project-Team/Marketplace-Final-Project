using Microsoft.AspNetCore.Mvc.Rendering;
namespace Marketplace.Web.Models
{
    public class CatalogPageViewModel
    {
        public CatalogFilterViewModel Filter { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public List<CatalogProductViewModel> Products { get; set; } = new();
        public string? SelectedCategoryName { get; set; }
        public string? SelectedCategoryImageUrl { get; set; }

        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }
}