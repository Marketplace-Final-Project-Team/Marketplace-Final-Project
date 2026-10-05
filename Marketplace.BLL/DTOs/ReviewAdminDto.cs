using Marketplace.Domain.Enums;

namespace Marketplace.BLL.DTOs
{
    public class ReviewAdminDto
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }

        public ReviewStatus Status { get; set; }
        public string? ModerationReason { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public bool HasPendingAppeal { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
