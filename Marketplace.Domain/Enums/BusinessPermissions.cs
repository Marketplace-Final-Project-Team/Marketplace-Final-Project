using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Authorization
{

    public static class BusinessPermissions
    {
        public static readonly BusinessMemberRole[] ProductCrud =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.ProductManager
        };

        public static readonly BusinessMemberRole[] Statistics =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.ProductManager,
            BusinessMemberRole.BusinessAnalyst
        };

        public static readonly BusinessMemberRole[] EditBusinessProfile =
        {
            BusinessMemberRole.Owner
        };
    }
}