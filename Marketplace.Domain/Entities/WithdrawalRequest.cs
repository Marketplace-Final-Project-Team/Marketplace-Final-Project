using System;
using System.Collections.Generic;
using System.Text;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
    public class WithdrawalRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BusinessId { get; set; }
        public Business Business { get; set; }
        public decimal Amount { get; set; } = 0;

        public string CardNumber { get; set; }
        public string RecipientName { get; set; } // імя отримувача

        public string? ProcessedByUserId { get; set; }

        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

    }
}
