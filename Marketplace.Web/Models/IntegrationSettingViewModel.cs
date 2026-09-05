namespace Marketplace.Web.Models
{
    public class IntegrationSettingViewModel
    {
        public string Category { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
        public List<IntegrationKeyViewModel> Keys { get; set; } = new();
    }
}
