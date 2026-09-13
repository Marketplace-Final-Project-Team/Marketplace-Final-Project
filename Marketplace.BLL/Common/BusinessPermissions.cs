using Marketplace.Domain.Enums;

namespace Marketplace.BLL.Common
{
    public static class BusinessPermissions
    {
        public static readonly BusinessMemberRole[] ProductCrud =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.Admin,
            BusinessMemberRole.ProductManager
        };

        public static readonly BusinessMemberRole[] Statistics =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.Admin,
            BusinessMemberRole.BusinessAnalyst
        };

        public static readonly BusinessMemberRole[] PromoCodes =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.Admin,
            BusinessMemberRole.ProductManager
        };

      
        public static readonly BusinessMemberRole[] ManageTeam =
        {
            BusinessMemberRole.Owner,
            BusinessMemberRole.Admin
        };

      
        public static readonly BusinessMemberRole[] EditBusinessProfile =
        {
            BusinessMemberRole.Owner
        };

 
        public static readonly BusinessMemberRole[] ManageSettings = EditBusinessProfile;
    }
}