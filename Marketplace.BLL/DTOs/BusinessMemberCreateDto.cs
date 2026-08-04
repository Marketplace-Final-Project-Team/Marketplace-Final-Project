using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class BusinessMemberCreateDto
    {
        public Guid BusinessId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; }  
    }
}
