namespace Marketplace.BLL.DTOs
{
    public class PromoCodeDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsSingleUse { get; set; }
        public bool IsUsed { get; set; }
        public bool IsActive { get; set; }
    }

    public class PromoCodeApplicationDto
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }
        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }
}
