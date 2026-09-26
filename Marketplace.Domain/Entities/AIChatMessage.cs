using Marketplace.Domain.Common;

namespace Marketplace.Domain.Entities
{
    public class AIChatMessage: BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public string Role { get; set; } = string.Empty;

        public string? Text { get; set; }
        public string? FunctionName { get; set; }
        public string? FunctionArgsJson { get; set; }
        public string? FunctionResponseJson { get; set; }

        public string? ThoughtSignature { get; set; }
    }
}
