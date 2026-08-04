using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Common
{
    public class BusinessMemberResult: ServiceResult
    {
        public string? UserId { get; set; }
        public string? Token { get; set; } 
        public string? Email { get; set; } 
        public BusinessMemberRole? Role { get; set; }
    }
}
