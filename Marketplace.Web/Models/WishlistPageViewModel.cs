namespace Marketplace.Web.Models;

public class WishlistPageViewModel
{
    public Guid Id { get; set; }
    public List<WishlistItemViewModel> Items { get; set; } = new();
    public int TotalItems => Items.Count;
}

public class WishlistItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public bool InStock { get; set; }
}
