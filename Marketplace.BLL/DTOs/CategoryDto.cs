namespace Marketplace.BLL.DTOs
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ActiveProductCount { get; set; }
        public bool IsAdultOnly { get; set; }
    }
}
