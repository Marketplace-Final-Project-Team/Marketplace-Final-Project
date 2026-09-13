using System.Collections.Generic;

namespace Marketplace.Web.Models
{
    public class PersonalDiscountViewModel
    {
        public string StoreName { get; set; } = string.Empty;
        public decimal Percent { get; set; }
        public DateTime EndDate { get; set; }
    }
}