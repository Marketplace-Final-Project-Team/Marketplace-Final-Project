namespace Marketplace.BLL.DTOs
{
    public class ReviewSummaryDto
    {
        public string? ProductName { get; set; } = string.Empty;
        public string? UserName { get; set; } = string.Empty;
        public float Rating { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } 
    }
}
