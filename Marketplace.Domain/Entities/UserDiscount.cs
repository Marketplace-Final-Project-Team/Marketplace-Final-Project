using Marketplace.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Entities
{
    public class UserDiscount : BaseEntity
    {
       
        public User User { get; set; }
        public string UserId { get; set; }

        public Business Business { get; set; }
        public Guid BusinessId { get; set; }

        public decimal Percent { get; set; } = 0;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive => StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow;
    }
}
