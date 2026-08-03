using Microsoft.AspNetCore.Http;

namespace Marketplace.BLL.DTOs
{
    public class UpdateProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public IFormFile? Avatar { get; set; }
    }
}
