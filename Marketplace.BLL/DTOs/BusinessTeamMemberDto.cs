using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class BusinessTeamMemberDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public BusinessMemberRole Role { get; set; }
        public bool IsAccepted { get; set; }
    }
}
