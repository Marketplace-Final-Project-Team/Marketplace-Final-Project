namespace Marketplace.Web.Models
{
    public class NotificationsViewModel
    {
        public List<ActivityItemViewModel> OrderActivity { get; set; } = new();
        public List<NotificationProductViewModel> NewProducts { get; set; } = new();
        public List<NotificationProductViewModel> RecommendedProducts { get; set; } = new();
    }

    public class NotificationProductViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
