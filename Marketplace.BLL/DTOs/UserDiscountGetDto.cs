using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class UserDiscountGetDto
    {
        public string UserId { get; set; }
        public string? UserName { get; set; }
        public Guid BusinessId { get; set; }
        public string? StoreName { get; set; }
        public decimal Percent { get; set; } = 0;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; }
    }
}
