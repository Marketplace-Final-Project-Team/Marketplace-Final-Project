using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class ReviewAppealAdminDto
    {
        public Guid Id { get; set; }

        public Guid ReviewId { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        public string OldBody { get; set; } = string.Empty;
        public float OldRating { get; set; }
        public string? RejectionReason { get; set; }

        public string NewBody { get; set; } = string.Empty;
        public float NewRating { get; set; }

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
