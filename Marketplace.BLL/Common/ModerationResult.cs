namespace Marketplace.BLL.Common
{
    public class ModerationResult
    {
        public bool IsApproved { get; set; }
        public bool ContainsProhibitedContent { get; set; }
        public bool ContainsAdultContent { get; set; }
        public string? Reason { get; set; }
    }
}
