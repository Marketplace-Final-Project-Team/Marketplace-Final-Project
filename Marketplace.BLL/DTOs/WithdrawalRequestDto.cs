using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.BLL.DTOs
{
    public class WithdrawalRequestDto
    {
        public Guid Id { get; set; }
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; }
        public decimal Amount { get; set; }
        public string CardNumber { get; set; }
        public string RecipientName { get; set; }
        public string Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ProcessedByUserId { get; set; }
        public string? Message { get; set; }
    }
}
