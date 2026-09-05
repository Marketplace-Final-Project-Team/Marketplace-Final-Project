using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class CategoryDiscountDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }

        public Guid BusinessId { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
