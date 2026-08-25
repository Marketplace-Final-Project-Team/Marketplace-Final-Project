namespace Marketplace.BLL.Common;

public class PagedResult<T>
{
	public IReadOnlyList<T> Items { get; set; } = new List<T>();
	public int Page { get; set; }
	public int TotalPages { get; set; }
	public int TotalItems { get; set; }
}