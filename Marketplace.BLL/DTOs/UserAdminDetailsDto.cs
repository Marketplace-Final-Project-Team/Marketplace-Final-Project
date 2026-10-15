using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class UserAdminDetailsDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
