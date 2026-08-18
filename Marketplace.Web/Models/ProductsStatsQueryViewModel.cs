namespace Marketplace.BLL.DTOs
{
    public class ProductsStatsQueryViewModel
    {
        public Guid BusinessId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsDescending { get; set; } = true;
    }
}
