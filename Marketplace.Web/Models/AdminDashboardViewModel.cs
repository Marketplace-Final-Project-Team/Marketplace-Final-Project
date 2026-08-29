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
        public decimal MarketplaceBalance { get; set; }
    }
}
