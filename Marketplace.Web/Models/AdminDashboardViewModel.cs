using Marketplace.BLL.DTOs;

namespace Marketplace.Web.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int BannedUsers { get; set; }
        public int TotalProducts { get; set; }
        public int PendingProducts { get; set; }
        public int BannedProducts { get; set; }
        public int TotalBusinesses { get; set; }
        public int PendingBusinesses { get; set; }
        public int PendingAdultRequests { get; set; }
        public int PendingWithdrawals { get; set; }
        public decimal MarketplaceBalance { get; set; }
        public List<BusinessAdminDto> AdultRequestsQueue { get; set; } = new();
    }
}
