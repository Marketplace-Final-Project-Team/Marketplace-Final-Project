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
        public List<CatalogCategoryTileViewModel> CategoryTiles { get; set; } = new();

        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
    }

    public class CatalogCategoryTileViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
    }
}