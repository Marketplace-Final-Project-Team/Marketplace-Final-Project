using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class ProfileUpdateViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        public IFormFile? Avatar { get; set; }
    }
}
