namespace Marketplace.BLL.DTOs
{
	public class NotificationItemDto
	{
		public string Type { get; set; } = string.Empty;
		public string Text { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public string LinkController { get; set; } = string.Empty;
		public string LinkAction { get; set; } = string.Empty;
		public Guid LinkId { get; set; }
	}
}