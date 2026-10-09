// File: Loan.cs
// Purpose: Defines the Loan domain entity (one borrow of one BookCopy by one Member).
// All state changes go through Return / Renew / MarkOverdue so rules like
// "no double return" and "max 2 renewals" always hold.
using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents one borrow of a physical <see cref="BookCopy"/> by a <see cref="Member"/>.
/// Tracks borrow date, due date, return date, status, and renewal count.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// One borrow of one BookCopy by one Member. All state changes (return/renew)
// go through methods so rules (no double-return, max 2 renewals) always hold.
public class Loan : BaseEntity
{
    /// <summary>
    /// The default loan length in days when no custom period is supplied.
    /// </summary>
    private const int DefaultLoanDays = 14;
    /// <summary>
    /// The maximum number of times a single loan may be renewed.
    /// </summary>
    private const int MaxRenewals = 2;

    /// <summary>
    /// Gets the foreign key of the borrowed copy.
    /// </summary>
    public Guid BookCopyId { get; private set; }
    /// <summary>
    /// Gets the navigation property to the borrowed copy. Set by EF Core on load.
    /// </summary>
    public BookCopy BookCopy { get; private set; } = null!;

    /// <summary>
    /// Gets the foreign key of the borrowing member.
    /// </summary>
    public Guid MemberId { get; private set; }
    /// <summary>
    /// Gets the navigation property to the borrowing member. Set by EF Core on load.
    /// </summary>
    public Member Member { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC date and time when the item was borrowed.
    /// </summary>
    public DateTime BorrowedAt { get; private set; }
    /// <summary>
    /// Gets the UTC date and time when the item is (or was) due back.
    /// Extended by <see cref="Renew"/>.
    /// </summary>
    public DateTime DueDate { get; private set; }
    /// <summary>
    /// Gets the UTC date and time when the item was returned. Null while still on loan.
    /// </summary>
    public DateTime? ReturnedAt { get; private set; }
    /// <summary>
    /// Gets the current lifecycle state of the loan.
    /// Changed only through <see cref="Return"/>, <see cref="Renew"/>, and <see cref="MarkOverdue"/>.
    /// </summary>
    public LoanStatus Status { get; private set; } = LoanStatus.Active;
    /// <summary>
    /// Gets how many times this loan has been renewed so far.
    /// </summary>
    public int RenewalCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Loan"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core re-hydration only.
    protected Loan()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Loan"/> class.
    /// </summary>
    /// <param name="bookCopyId">Id of the copy being borrowed. Cannot be empty GUID.</param>
    /// <param name="memberId">Id of the borrowing member. Cannot be empty GUID.</param>
    /// <param name="borrowedAt">Optional borrow time. Defaults to now (UTC) when null.</param>
    /// <param name="loanDays">Loan length in days. Must be positive.</param>
    /// <exception cref="DomainException">Thrown when any argument breaks a loan rule.</exception>
    public Loan(Guid bookCopyId, Guid memberId, DateTime? borrowedAt = null, int loanDays = DefaultLoanDays)
    {
        // Guards: a loan must link a real copy to a real member for a positive period.
        if (bookCopyId == Guid.Empty)
            throw new DomainException("Book copy is required.");
        if (memberId == Guid.Empty)
            throw new DomainException("Member is required.");
        if (loanDays <= 0)
            throw new DomainException("Loan period must be positive.");

        BookCopyId = bookCopyId;
        MemberId = memberId;
        // Allow tests/back-dated imports to pass a borrow time; normal flow uses UtcNow.
        BorrowedAt = borrowedAt ?? DateTime.UtcNow;
        // Due date is simply borrow time plus the loan period.
        DueDate = BorrowedAt.AddDays(loanDays);
        Status = LoanStatus.Active;
    }

    /// <summary>
    /// Marks this loan as returned and records when it came back.
    /// </summary>
    /// <param name="returnedAt">Optional return time. Defaults to now (UTC) when null.</param>
    /// <exception cref="DomainException">Thrown when the loan was already returned.</exception>
    public void Return(DateTime? returnedAt = null)
    {
        // Guard: returning twice would corrupt history, so refuse the second call.
        if (Status == LoanStatus.Returned)
            throw new DomainException("Loan is already returned.");

        ReturnedAt = returnedAt ?? DateTime.UtcNow;
        Status = LoanStatus.Returned;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Extends the due date and counts one renewal.
    /// </summary>
    /// <param name="extraDays">Days to add to the due date. Defaults to 14.</param>
    /// <exception cref="DomainException">Thrown when the loan is not active or the renewal limit is reached.</exception>
    public void Renew(int extraDays = DefaultLoanDays)
    {
        // Guard: only active loans can be extended; returned/overdue/lost loans cannot.
        if (Status != LoanStatus.Active)
            throw new DomainException("Only active loans can be renewed.");
        // Guard: library policy caps renewals so popular books come back.
        if (RenewalCount >= MaxRenewals)
            throw new DomainException($"Loan cannot be renewed more than {MaxRenewals} times.");

        DueDate = DueDate.AddDays(extraDays);
        RenewalCount++;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Marks an active loan as overdue (normally called by a background job).
    /// </summary>
    /// <exception cref="DomainException">Thrown when the loan is not active.</exception>
    public void MarkOverdue()
    {
        // Guard: only active loans can flip to overdue; returned loans stay returned.
        if (Status != LoanStatus.Active)
            throw new DomainException("Only active loans can be marked overdue.");

        Status = LoanStatus.Overdue;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }

    /// <summary>
    /// Checks whether this loan is currently late.
    /// </summary>
    /// <param name="now">Optional "current" time for testing. Defaults to now (UTC) when null.</param>
    /// <returns>True when the loan is still active and the due date has passed.</returns>
    // Used to flag late loans (e.g. a nightly job calling MarkOverdue).
    public bool IsOverdue(DateTime? now = null) =>
        // Late means: still checked out AND due date is in the past.
        Status == LoanStatus.Active && DueDate < (now ?? DateTime.UtcNow);
}
