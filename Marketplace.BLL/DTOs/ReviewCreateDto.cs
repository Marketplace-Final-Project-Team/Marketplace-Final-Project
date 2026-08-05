namespace Marketplace.BLL.DTOs
{
    public class ReviewCreateDto
    {
        public Guid ProductId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }
    }
}
