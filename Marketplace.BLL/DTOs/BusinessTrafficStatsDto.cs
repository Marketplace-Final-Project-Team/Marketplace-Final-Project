namespace Marketplace.BLL.DTOs
{
    public class BusinessTrafficStatsDto
    {
        public int TotalViews { get; set; }
        public int TotalCartAdds { get; set; }
        public int TotalWishlistAdds { get; set; }
        public List<ProductTrafficStatsDto> ByProduct { get; set; } = new();
    }

    public class ProductTrafficStatsDto
    {
        public Guid ProductId { get; set; }
        public int Views { get; set; }
        public int CartAdds { get; set; }
        public int WishlistAdds { get; set; }
    }
}
