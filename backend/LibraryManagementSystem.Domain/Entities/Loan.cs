using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// One borrow of one BookCopy by one Member. All state changes (return/renew)
// go through methods so rules (no double-return, max 2 renewals) always hold.
public class Loan : BaseEntity
{
    private const int DefaultLoanDays = 14;
    private const int MaxRenewals = 2;

    public Guid BookCopyId { get; private set; }
    public BookCopy BookCopy { get; private set; } = null!;

    public Guid MemberId { get; private set; }
    public Member Member { get; private set; } = null!;

    public DateTime BorrowedAt { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnedAt { get; private set; }
    public LoanStatus Status { get; private set; } = LoanStatus.Active;
    public int RenewalCount { get; private set; }

    // EF Core re-hydration only.
    protected Loan()
    {
    }

    public Loan(Guid bookCopyId, Guid memberId, DateTime? borrowedAt = null, int loanDays = DefaultLoanDays)
    {
        if (bookCopyId == Guid.Empty)
            throw new DomainException("Book copy is required.");
        if (memberId == Guid.Empty)
            throw new DomainException("Member is required.");
        if (loanDays <= 0)
            throw new DomainException("Loan period must be positive.");

        BookCopyId = bookCopyId;
        MemberId = memberId;
        BorrowedAt = borrowedAt ?? DateTime.UtcNow;
        DueDate = BorrowedAt.AddDays(loanDays);
        Status = LoanStatus.Active;
    }

    public void Return(DateTime? returnedAt = null)
    {
        if (Status == LoanStatus.Returned)
            throw new DomainException("Loan is already returned.");

        ReturnedAt = returnedAt ?? DateTime.UtcNow;
        Status = LoanStatus.Returned;
        Touch();
    }

    public void Renew(int extraDays = DefaultLoanDays)
    {
        if (Status != LoanStatus.Active)
            throw new DomainException("Only active loans can be renewed.");
        if (RenewalCount >= MaxRenewals)
            throw new DomainException($"Loan cannot be renewed more than {MaxRenewals} times.");

        DueDate = DueDate.AddDays(extraDays);
        RenewalCount++;
        Touch();
    }

    public void MarkOverdue()
    {
        if (Status != LoanStatus.Active)
            throw new DomainException("Only active loans can be marked overdue.");

        Status = LoanStatus.Overdue;
        Touch();
    }

    // Used to flag late loans (e.g. a nightly job calling MarkOverdue).
    public bool IsOverdue(DateTime? now = null) =>
        Status == LoanStatus.Active && DueDate < (now ?? DateTime.UtcNow);
}
