namespace Marketplace.Web.Models;

public class CatalogProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreLogoUrl { get; set; } = string.Empty;
    public string StoreDescription { get; set; } = string.Empty;
    public List<string> AdditionalImageUrls { get; set; } = new();
    public DateTime EstimatedDeliveryStart { get; set; }
    public DateTime EstimatedDeliveryEnd { get; set; }
    public List<CatalogRelatedProductViewModel> RelatedProducts { get; set; } = new();

    public List<Marketplace.BLL.DTOs.ReviewDto> Reviews { get; set; } = new();
    public string? CurrentUserId { get; set; }
    public bool IsInWishlist { get; set; }

    // Set directly from the catalog query's aggregate (ProductService.GetCatalogPageAsync) on
    // the listing page, where loading each product's full Reviews list would be wasteful; on
    // the details page these are computed from Reviews after it's populated.
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }
    public Marketplace.BLL.DTOs.ReviewDto? CurrentUserReview => string.IsNullOrEmpty(CurrentUserId)
        ? null
        : Reviews.FirstOrDefault(r => r.UserId == CurrentUserId);

    public IReadOnlyList<string> GalleryImageUrls => new[] { MainImageUrl }
        .Concat(AdditionalImageUrls)
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

public class CatalogRelatedProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string MainImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
