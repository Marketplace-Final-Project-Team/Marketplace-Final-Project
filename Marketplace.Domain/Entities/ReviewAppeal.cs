using System.ComponentModel.DataAnnotations.Schema;
using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class ReviewAppeal : BaseEntity
    {
        public Guid ReviewId { get; set; }

        [ForeignKey(nameof(ReviewId))]
        public Review Review { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public string OriginalBody { get; set; } = string.Empty;
        public float OriginalRating { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(1000)]
        public string? OriginalModerationReason { get; set; }

        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }

        public ReviewAppealStatus Status { get; set; } = ReviewAppealStatus.Pending;

        [System.ComponentModel.DataAnnotations.MaxLength(450)]
        public string? ProcessedByAdminId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(1000)]
        public string? DecisionReason { get; set; }
    }
}
