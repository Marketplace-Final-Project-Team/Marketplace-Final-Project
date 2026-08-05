using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models
{
    public class UserItemAdminViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
    }
}
