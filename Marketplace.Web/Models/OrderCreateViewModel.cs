using System.ComponentModel.DataAnnotations;

namespace Marketplace.Web.Models
{
    public class OrderCreateViewModel
    {
        [Required]
        public Guid AddressId { get; set; }
    }
}
