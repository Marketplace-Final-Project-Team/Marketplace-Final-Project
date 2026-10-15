namespace Marketplace.BLL.DTOs.Gemini
{
    public class GeminiModerationResponse
    {
        public bool IsAdult { get; set; }
        public bool IsViolent { get; set; }
        public bool ContainsProhibited { get; set; }
        public string? Reason { get; set; }
    }
    public class GeminiReturnModerationResponse
    {
        public bool ShouldApprove { get; set; }
        public bool IsConfident { get; set; }
        public string? Reason { get; set; }
    }
}

   

