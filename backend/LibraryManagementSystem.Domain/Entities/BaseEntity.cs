// Shared base for every entity: one place for the Id + audit timestamps.
// Interview point: generic repositories constrain to BaseEntity (IRepository<T> where T : BaseEntity).
namespace LibraryManagementSystem.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    // Bump UpdatedAt. Protected so only the entity itself can change audit data,
    // and only via its behavior methods (e.g. Update, Return).
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
