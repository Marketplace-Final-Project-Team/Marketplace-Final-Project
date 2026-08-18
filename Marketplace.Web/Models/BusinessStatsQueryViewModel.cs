namespace Marketplace.Web.Models
{
    public class BusinessStatsQueryViewModel
    {
        public Guid BusinessId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsDescending {  get; set; }
    }
}
