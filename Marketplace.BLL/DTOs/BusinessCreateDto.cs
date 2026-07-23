using Microsoft.AspNetCore.Http;
namespace Marketplace.BLL.DTOs
{
    public class BusinessCreateDto
    {
        public string StoreName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IFormFile Logo { get; set; } = null!;
        public IFormFile Banner { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
    }
}
