using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class BusinessAdminDto
    {
       public Guid Id { get; set; }
        public string? Name { get; set; }
        public string LogoUrl { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public BusinessStatus Status { get; set; }
    }
}
