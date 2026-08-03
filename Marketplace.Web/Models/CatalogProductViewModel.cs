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
    public List<string> AdditionalImageUrls { get; set; } = new();
}
