// File: BaseEntity.cs
// Purpose: Defines the shared base class for all domain entities.
// It provides a unique Id plus CreatedAt / UpdatedAt audit timestamps,
// so every entity gets identity and auditing the same way.
namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Base class for all domain entities.
/// Provides a unique identifier and creation / update timestamps.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// A new GUID is generated automatically when the entity is created.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();
    /// <summary>
    /// Gets the UTC date and time when this entity was created.
    /// Set once at construction and never changed afterwards.
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets the UTC date and time when this entity was last updated.
    /// Null means the entity has not been modified since creation.
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Updates the <see cref="UpdatedAt"/> timestamp to the current UTC time.
    /// Should be called at the end of every method that changes the entity.
    /// </summary>
    // Bump UpdatedAt. Protected so only the entity itself can change audit data,
    // and only via its behavior methods (e.g. Update, Return).
    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
