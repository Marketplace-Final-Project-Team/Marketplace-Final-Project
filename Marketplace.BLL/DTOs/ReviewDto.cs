namespace Marketplace.BLL.DTOs
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        public DateTime PublicationDate { get; set; }
    }
}
