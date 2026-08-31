namespace Marketplace.DAL.DTOs
{
    public class CommissionForBusinessDto
    {
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public decimal TotalCommission { get; set; }
    }
}
