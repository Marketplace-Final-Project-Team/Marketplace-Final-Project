namespace Marketplace.Web.Models
{
    public class HomeCategoryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ProductCount { get; set; }
    }

    public class HomeCategoryProductsViewModel
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<CatalogProductViewModel> Products { get; set; } = new();
    }

    public class HomeViewModel
    {
        public List<HomeCategoryViewModel> Categories { get; set; } = new();
        public List<HomeCategoryProductsViewModel> CategorySections { get; set; } = new();
        public List<CatalogRelatedProductViewModel> RecentlyViewed { get; set; } = new();

        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public string? SortBy { get; set; }
    }
}