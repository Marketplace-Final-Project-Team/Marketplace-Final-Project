namespace Marketplace.Web.Models;

public class IntegrationKeyViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string MaskedValue { get; set; } = string.Empty;
    public bool IsSecret { get; set; }
    public bool IsOverridden { get; set; }
}

public class IntegrationSettingViewModel
{
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public List<IntegrationKeyViewModel> Keys { get; set; } = new();
}
