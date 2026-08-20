namespace Marketplace.BLL.DTOs.Products;

public class CatalogProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? MainImageUrl { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }

    public bool HasActiveDiscount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public decimal? DiscountedPrice { get; set; }
}