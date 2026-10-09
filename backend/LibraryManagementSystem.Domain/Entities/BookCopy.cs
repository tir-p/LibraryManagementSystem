// File: BookCopy.cs
// Purpose: Defines the BookCopy domain entity (one physical, borrowable item on a shelf).
// Loans point at a BookCopy, so one Book title can have many copies with independent availability.
using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents a single physical copy of a <see cref="Book"/> title.
/// This is the item that gets loaned out, reserved, or marked lost/damaged.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// One physical, borrowable item on a shelf. This is what Loans point at,
// so one Book title can have many copies with independent availability.
public class BookCopy : BaseEntity
{
    /// <summary>
    /// Gets the foreign key of the catalog title this copy belongs to.
    /// </summary>
    public Guid BookId { get; private set; }
    /// <summary>
    /// Gets the navigation property to the catalog title. Set by EF Core on load.
    /// </summary>
    public Book Book { get; private set; } = null!;

    /// <summary>
    /// Gets the unique physical label for this copy (for example, "BK-0001").
    /// Uniqueness is enforced in the EF Core configuration.
    /// </summary>
    // Unique physical label (e.g. "BK-0001"), enforced in EF config.
    public string Barcode { get; private set; }
    /// <summary>
    /// Gets the optional shelf location (for example, "Aisle 3, Shelf B"). May be null.
    /// </summary>
    public string? ShelfLocation { get; private set; }
    /// <summary>
    /// Gets the current availability state of this copy.
    /// Changed only through <see cref="MarkLoaned"/> and <see cref="MarkAvailable"/>.
    /// </summary>
    public CopyStatus Status { get; private set; } = CopyStatus.Available;

    /// <summary>
    /// Gets a value indicating whether this copy can be borrowed right now.
    /// True only when <see cref="Status"/> is Available. Has no setter (computed).
    /// </summary>
    // Derived state, no setter: EF is told to Ignore it (see BookCopyConfiguration).
    public bool IsAvailable => Status == CopyStatus.Available;

    /// <summary>
    /// Gets the loan history for this copy.
    /// Navigation property used by EF Core for the one-to-many side.
    /// </summary>
    public ICollection<Loan> Loans { get; private set; } = new List<Loan>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BookCopy"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core re-hydration only.
    protected BookCopy()
    {
        // Safe default so the non-nullable Barcode is never null after EF creates the object.
        Barcode = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BookCopy"/> class.
    /// </summary>
    /// <param name="bookId">Id of the catalog title. Cannot be empty GUID.</param>
    /// <param name="barcode">Unique physical label. Cannot be empty.</param>
    /// <param name="shelfLocation">Optional shelf location.</param>
    /// <exception cref="DomainException">Thrown when a required value is missing.</exception>
    public BookCopy(Guid bookId, string barcode, string? shelfLocation = null)
    {
        // Guards: a copy must belong to a book and have a scannable barcode.
        if (bookId == Guid.Empty)
            throw new DomainException("Book is required.");
        if (string.IsNullOrWhiteSpace(barcode))
            throw new DomainException("Barcode is required.");

        BookId = bookId;
        // Trim to avoid hidden spaces that would break barcode scans/lookups.
        Barcode = barcode.Trim();
        ShelfLocation = shelfLocation;
    }

    /// <summary>
    /// Marks this copy as loaned out. Only an available copy can be loaned.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the copy is not available.</exception>
    // Called by LoanService.Borrow; refuses double-loans at the domain level.
    public void MarkLoaned()
    {
        // Guard: prevents the same physical copy being loaned twice at once.
        if (!IsAvailable)
            throw new DomainException("Copy is not available for loan.");

        Status = CopyStatus.Loaned;
        // Refresh UpdatedAt so we know when availability last changed.
        Touch();
    }

    /// <summary>
    /// Marks this copy as available again (for example, after a return).
    /// </summary>
    // Called by LoanService.Return.
    public void MarkAvailable()
    {
        Status = CopyStatus.Available;
        // Refresh UpdatedAt so we know when availability last changed.
        Touch();
    }
}
