namespace Marketplace.BLL.DTOs
{
    public class ReviewDto
    {
        public Guid UserId { get; set; }
        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        public DateTime PublicationDate { get; set; }
    }
}
