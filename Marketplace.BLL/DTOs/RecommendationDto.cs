using Marketplace.Domain.Enums;
using System;

namespace Marketplace.BLL.DTOs
{
    public class RecommendationDto
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal DiscountPercent { get; set; }
        public decimal FinalPrice { get; set; }
        

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public Guid BusinessId { get; set; }
        public string StoreName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public bool IsAdultOnly { get; set; }

        public int Score { get; set; }
    }
}