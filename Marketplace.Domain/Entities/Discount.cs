using Marketplace.Domain.Common;
using Marketplace.Domain.Enums;

namespace Marketplace.Domain.Entities
{
	public class Discount : BaseEntity
	{
		public Guid? ProductId { get; set; }
		public Product? Product { get; set; } = null!;

		public Guid? BusinessId { get; set; }
		public Business? Business { get; set; } = null!;

		public Guid? CategoryId { get; set; }
		public Category? Category { get; set; } = null!;

        public decimal Percent { get; set; }

		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
        public bool IsActive => StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow;

		public DiscountType DiscountType { get; set; }
    }
}