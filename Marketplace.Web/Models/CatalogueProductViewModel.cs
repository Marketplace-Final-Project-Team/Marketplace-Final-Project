namespace Marketplace.Web.Models;

public class CatalogueProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? Sku { get; set; }
    public List<CatalogProductVariantViewModel> Variants { get; set; } = new();
    public int LowStockThreshold { get; set; } = 5;
    public DateTime CreatedAt { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string StoreLogoUrl { get; set; } = string.Empty;
    public string StoreDescription { get; set; } = string.Empty;
    public List<string> AdditionalImageUrls { get; set; } = new();
    public DateTime EstimatedDeliveryStart { get; set; }
    public DateTime EstimatedDeliveryEnd { get; set; }
    public bool IsAgeRestricted { get; set; }
    public List<CatalogRelatedProductViewModel> RelatedProducts { get; set; } = new();

    public List<Marketplace.BLL.DTOs.ReviewDto> Reviews { get; set; } = new();
    public string? CurrentUserId { get; set; }
    public bool IsInWishlist { get; set; }

    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }

    public Marketplace.BLL.DTOs.ReviewDto? CurrentUserReview =>
        !string.IsNullOrEmpty(CurrentUserId)
            ? Reviews.FirstOrDefault(r => r.UserId == CurrentUserId)
            : null;

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool HasDiscount => OriginalPrice.HasValue && DiscountPercent.HasValue;

    public IReadOnlyList<string> GalleryImageUrls => new[] { MainImageUrl }
        .Concat(AdditionalImageUrls)
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

public class CatalogRelatedProductViewModel
{
    public decimal? OriginalPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string MainImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CatalogProductVariantViewModel
{
    public Guid Id { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}