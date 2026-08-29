namespace Marketplace.Web.Models;

public class UserActivityLogItemViewModel
{
    public string UserEmail { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BusinessActivityLogItemViewModel
{
    public string BusinessName { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
