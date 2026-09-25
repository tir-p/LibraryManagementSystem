using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// One physical, borrowable item on a shelf. This is what Loans point at,
// so one Book title can have many copies with independent availability.
public class BookCopy : BaseEntity
{
    public Guid BookId { get; private set; }
    public Book Book { get; private set; } = null!;

    // Unique physical label (e.g. "BK-0001"), enforced in EF config.
    public string Barcode { get; private set; }
    public string? ShelfLocation { get; private set; }
    public CopyStatus Status { get; private set; } = CopyStatus.Available;

    // Derived state, no setter: EF is told to Ignore it (see BookCopyConfiguration).
    public bool IsAvailable => Status == CopyStatus.Available;

    public ICollection<Loan> Loans { get; private set; } = new List<Loan>();

    // EF Core re-hydration only.
    protected BookCopy()
    {
        Barcode = string.Empty;
    }

    public BookCopy(Guid bookId, string barcode, string? shelfLocation = null)
    {
        if (bookId == Guid.Empty)
            throw new DomainException("Book is required.");
        if (string.IsNullOrWhiteSpace(barcode))
            throw new DomainException("Barcode is required.");

        BookId = bookId;
        Barcode = barcode.Trim();
        ShelfLocation = shelfLocation;
    }

    // Called by LoanService.Borrow; refuses double-loans at the domain level.
    public void MarkLoaned()
    {
        if (!IsAvailable)
            throw new DomainException("Copy is not available for loan.");

        Status = CopyStatus.Loaned;
        Touch();
    }

    // Called by LoanService.Return.
    public void MarkAvailable()
    {
        Status = CopyStatus.Available;
        Touch();
    }
}
