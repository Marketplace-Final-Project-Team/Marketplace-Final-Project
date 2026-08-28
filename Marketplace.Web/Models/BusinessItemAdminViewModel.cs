using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class BusinessItemAdminViewModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string LogoUrl { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public BusinessStatus Status { get; set; }
        public decimal CommissionPercent { get; set; }
    }
}
