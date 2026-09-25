using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Marketplace.Domain.Entities
{
    public class Review : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        public string Body { get; set; } = string.Empty;
        public float Rating { get; set; }
        public ReviewStatus Status { get; set; } = ReviewStatus.Approved;

        [MaxLength(450)]
        public string? ModeratedByAdminId { get; set; }
        public DateTime? ModeratedAt { get; set; }
        [MaxLength(1000)]
        public string? ModerationReason { get; set; }
    }
}
