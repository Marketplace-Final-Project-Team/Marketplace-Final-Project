using Marketplace.Domain.Enums;

namespace Marketplace.Web.Models.Businesses
{
    public static class BusinessRoleContext
    {
        public static string PanelLabel(BusinessMemberRole? role) => role switch
        {
            BusinessMemberRole.Owner => "Панель продавця",
            BusinessMemberRole.Admin => "Панель адміністратора",
            BusinessMemberRole.ProductManager => "Панель менеджера товарів",
            BusinessMemberRole.BusinessAnalyst => "Панель аналітика",
            _ => "Панель продавця"
        };

        public static bool CanManageSettings(BusinessMemberRole? role) => role == BusinessMemberRole.Owner;
        public static bool CanManageTeam(BusinessMemberRole? role) => role == BusinessMemberRole.Owner;
        public static bool CanRequestPayout(BusinessMemberRole? role) => role == BusinessMemberRole.Owner;
    }
}