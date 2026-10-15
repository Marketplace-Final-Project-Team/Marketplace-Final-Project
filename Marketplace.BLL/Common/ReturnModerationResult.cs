namespace Marketplace.BLL.Common
{
    public class ReturnModerationResult
    {

        public bool ShouldApprove { get; set; }

        public bool IsConfident { get; set; }

        public string? Reason { get; set; }
    }
}
