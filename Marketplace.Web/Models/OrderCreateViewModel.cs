using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class OrderCreateViewModel
    {
        [Required]
        public Guid AddressId { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        
    }
}
