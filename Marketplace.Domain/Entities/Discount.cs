using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
	public class Discount : BaseEntity
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid ProductId { get; set; }
		public Product Product { get; set; } = null!;

		public int Percent { get; set; }

		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
        public bool IsActive => StartDate <= DateTime.UtcNow && EndDate >= DateTime.UtcNow;
    }
}