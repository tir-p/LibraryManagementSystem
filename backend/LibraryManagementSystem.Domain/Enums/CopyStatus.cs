// File: CopyStatus.cs
// Purpose: Defines the availability states for one physical BookCopy.
// Stored as strings in the database (see BookCopyConfiguration).
namespace LibraryManagementSystem.Domain.Enums;

/// <summary>
/// Availability of one physical <see cref="Entities.BookCopy"/>.
/// </summary>
// Availability of one physical BookCopy. Stored as strings (see BookCopyConfiguration).
public enum CopyStatus
{
    /// <summary>
    /// The copy is on the shelf and can be borrowed.
    /// </summary>
    Available = 0,
    /// <summary>
    /// The copy is currently checked out on a loan.
    /// </summary>
    Loaned = 1,
    /// <summary>
    /// The copy is held for a member and cannot be borrowed by others.
    /// </summary>
    Reserved = 2,
    /// <summary>
    /// The copy is reported lost and cannot be borrowed.
    /// </summary>
    Lost = 3,
    /// <summary>
    /// The copy is damaged and cannot be borrowed until repaired or replaced.
    /// </summary>
    Damaged = 4
}
