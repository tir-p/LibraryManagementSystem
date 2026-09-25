namespace LibraryManagementSystem.Domain.Enums;

// Availability of one physical BookCopy. Stored as strings (see BookCopyConfiguration).
public enum CopyStatus
{
    Available = 0,
    Loaned = 1,
    Reserved = 2,
    Lost = 3,
    Damaged = 4
}
