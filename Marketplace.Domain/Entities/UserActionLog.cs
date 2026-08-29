namespace Marketplace.Domain.Entities
{
    // Deliberately not : BaseEntity — an audit trail is append-only, so it
    // has no IsDeleted/UpdatedAt and no soft-delete query filter.
    public class UserActionLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? RelatedEntityId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
