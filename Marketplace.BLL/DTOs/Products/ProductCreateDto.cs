using Microsoft.AspNetCore.Http;
namespace Marketplace.BLL.DTOs.Products
{
    public class ProductCreateDto
    {
        public Guid BusinessId { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? AdditionalImages { get; set; }
    }
}