namespace Marketplace.Web.Models
{
    public class IntegrationKeyViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsSecret { get; set; }
        public bool IsOverridden { get; set; }
        public string MaskedValue { get; set; } = string.Empty;
    }
}
