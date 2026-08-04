using Marketplace.Domain.Enums;
using Microsoft.Identity.Client;

namespace Marketplace.BLL.DTOs
{
    public class UserAdminDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
    }
}
