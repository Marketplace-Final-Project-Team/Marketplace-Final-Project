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
