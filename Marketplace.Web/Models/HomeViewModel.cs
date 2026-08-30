namespace Marketplace.Web.Models
{
    public class HomeCategoryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class HomeViewModel
    {
        public List<CatalogProductViewModel> FeaturedProducts { get; set; } = new();
        public List<HomeCategoryViewModel> Categories { get; set; } = new();
        public List<CatalogRelatedProductViewModel> RecentlyViewed { get; set; } = new();
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public string? SortBy { get; set; }

        public CatalogProductViewModel? TopProduct => FeaturedProducts.FirstOrDefault();
    }
}