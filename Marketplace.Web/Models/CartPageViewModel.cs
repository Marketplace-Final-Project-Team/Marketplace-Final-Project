using Marketplace.BLL.DTOs;
using Marketplace.BLL.Interfaces;

namespace Marketplace.Web.Models;

public class CartItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string? VariantSize { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public class CartSellerGroupViewModel
{
    public Guid BusinessId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string? StoreLogoUrl { get; set; }
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public class CartPageViewModel
{
    public List<CartSellerGroupViewModel> SellerGroups { get; set; } = new();
    public decimal Total => SellerGroups.Sum(g => g.Subtotal);
    public int TotalUnits => SellerGroups.Sum(g => g.ItemCount);
    public bool HasMultipleSellers => SellerGroups.Count > 1;
}

// Shared by CartController and OrderController — both need the same cart-items-grouped-by-
// seller view, and CartItemDto (from ICartService) carries no business/store info, so each
// item's real Product is looked up to find out who is actually selling it.
public static class CartGrouping
{
    public static async Task<List<CartSellerGroupViewModel>> GroupBySellerAsync(
        IReadOnlyList<CartItemDto> items,
        IProductService productService)
    {
        var groups = new Dictionary<Guid, CartSellerGroupViewModel>();

        foreach (var item in items)
        {
            var product = await productService.GetByIdAsync(item.ProductId);
            var businessId = product?.BusinessId ?? Guid.Empty;

            if (!groups.TryGetValue(businessId, out var group))
            {
                group = new CartSellerGroupViewModel
                {
                    BusinessId = businessId,
                    StoreName = product?.Business?.StoreName ?? "Marketplace",
                    StoreLogoUrl = product?.Business?.LogoUrl
                };
                groups[businessId] = group;
            }

            group.Items.Add(new CartItemViewModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                ImageUrl = item.ImageUrl,
                Quantity = item.Quantity,
                StockQuantity = item.StockQuantity,
                ProductVariantId = item.ProductVariantId,
                VariantSize = item.VariantSize
            });
        }

        return groups.Values.OrderBy(g => g.StoreName).ToList();
    }
}
