using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class SystemSetting : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsSecret { get; set; }
    }
}
