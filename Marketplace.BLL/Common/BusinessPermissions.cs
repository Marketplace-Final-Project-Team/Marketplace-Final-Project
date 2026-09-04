using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Common
{
    public static class BusinessPermissions
    {
        public static readonly BusinessMemberRole[] ManageSettings =
        {
            BusinessMemberRole.Owner
        };

        public static readonly BusinessMemberRole[] ManageTeam =
        {
            BusinessMemberRole.Owner
        };

        public static readonly BusinessMemberRole[] ViewBalance =
        {
            BusinessMemberRole.Owner
        };

        public static readonly BusinessMemberRole[] OwnerAndAdmin =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.Admin
        };
    }
}