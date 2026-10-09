using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// -----------------------------------------------------------------------------
// File header:
// LoanService is the heart of the system: borrow / return / renew. Rules live
// in the entities (BookCopy.MarkLoaned, Loan.Return/Renew, Member.CanBorrow);
// this service only loads entities, calls those methods, and saves ONCE so
// Loan + BookCopy changes commit atomically via the shared DbContext.
// Reads also auto-flag overdue loans via RefreshOverdueAsync.
// -----------------------------------------------------------------------------
// The heart of the system: borrow / return / renew.
// Rules live in the entities (BookCopy.MarkLoaned, Loan.Return/Renew, Member.CanBorrow);
// this service only loads entities, calls those methods, and saves ONCE,
// so Loan + BookCopy changes commit atomically via the shared DbContext.
/// <summary>
/// Loan use cases (borrow, return, renew, plus reads with overdue refresh).
/// </summary>
public class LoanService : ILoanService
{
    // Repositories share one DbContext, so one SaveChanges commits Loan+Copy together.
    private readonly IRepository<Loan> _loans;
    private readonly IRepository<BookCopy> _copies;
    private readonly IRepository<Member> _members;
    private readonly IRepository<Book> _books;

    /// <summary>
    /// Initializes the service with all repositories it needs (injected by DI).
    /// </summary>
    /// <param name="loans">Loan repository.</param>
    /// <param name="copies">BookCopy repository.</param>
    /// <param name="members">Member repository.</param>
    /// <param name="books">Book repository (for title display).</param>
    public LoanService(
        IRepository<Loan> loans,
        IRepository<BookCopy> copies,
        IRepository<Member> members,
        IRepository<Book> books)
    {
        _loans = loans;
        _copies = copies;
        _members = members;
        _books = books;
    }

    /// <summary>Gets all loans, refreshing overdue flags first.</summary>
    /// <returns>All loans as DTOs with display names.</returns>
    public async Task<IReadOnlyList<LoanDto>> GetAllAsync()
    {
        // Load all loans, then flag any that just became overdue.
        var loans = await _loans.ListAsync();
        await RefreshOverdueAsync(loans);
        // Bulk-load barcodes/titles/names once, then map.
        var lookups = await LookupDisplayData();
        return loans.Select(l => ToDto(l, lookups)).ToList();
    }

    /// <summary>Gets one loan by ID (with overdue refresh).</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The matching loan DTO.</returns>
    public async Task<LoanDto> GetByIdAsync(Guid id)
    {
        // 404 if missing; refresh single row so status is never stale.
        var loan = await GetLoanOrThrow(id);
        await RefreshOverdueAsync(new[] { loan });
        return ToDto(loan, await LookupDisplayData());
    }

    /// <summary>Borrows an available copy for an eligible member.</summary>
    /// <param name="request">Copy ID, member ID, loan days.</param>
    /// <returns>The created loan DTO.</returns>
    public async Task<LoanDto> BorrowAsync(BorrowRequest request)
    {
        // Load both sides; 404 if either ID is wrong.
        var member = await _members.GetByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException($"Member {request.MemberId} not found.");
        var copy = await _copies.GetByIdAsync(request.BookCopyId)
            ?? throw new KeyNotFoundException($"Book copy {request.BookCopyId} not found.");

        // Domain guards: available copy + member allowed to borrow (active, under cap).
        if (!copy.IsAvailable)
            throw new DomainException("Copy is not available for loan.");

        // Count active (non-returned) loans to enforce the borrow limit.
        var activeCount = await CountActiveLoans(member.Id);
        if (!member.CanBorrow(activeCount))
            throw new DomainException("Member cannot borrow more books.");

        // Create loan (computes DueDate) and flip copy to Loaned.
        var loan = new Loan(copy.Id, member.Id, DateTime.UtcNow, request.LoanDays);
        copy.MarkLoaned();

        // Save both together atomically (shared DbContext).
        await _loans.AddAsync(loan);
        _copies.Update(copy);
        await _loans.SaveChangesAsync();

        return ToDto(loan, await LookupDisplayData());
    }

    /// <summary>Returns a borrowed copy.</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The updated (Returned) loan DTO.</returns>
    public async Task<LoanDto> ReturnAsync(Guid id)
    {
        // Load loan + its copy (both must exist).
        var loan = await GetLoanOrThrow(id);
        var copy = await _copies.GetByIdAsync(loan.BookCopyId)
            ?? throw new KeyNotFoundException($"Book copy {loan.BookCopyId} not found.");

        // Domain transitions: loan -> Returned (sets ReturnedAt), copy -> Available.
        loan.Return();
        copy.MarkAvailable();

        // Persist both changes together.
        _loans.Update(loan);
        _copies.Update(copy);
        await _loans.SaveChangesAsync();

        return ToDto(loan, await LookupDisplayData());
    }

    /// <summary>Renews a loan, extending its due date.</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The renewed loan DTO.</returns>
    public async Task<LoanDto> RenewAsync(Guid id)
    {
        // Load or 404; Renew validates (returned/overdue/max-2 -> 400).
        var loan = await GetLoanOrThrow(id);
        loan.Renew(); // Max 2 renewals enforced inside.
        _loans.Update(loan);
        await _loans.SaveChangesAsync();
        return ToDto(loan, await LookupDisplayData());
    }

    /// <summary>Loads a loan or throws 404.</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The loan entity.</returns>
    private async Task<Loan> GetLoanOrThrow(Guid id)
        => await _loans.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Loan {id} not found.");

    // Flags late loans as Overdue on read, so the status is never stale.
    // Runs inside GetAll/GetById (one extra save only when something actually became overdue).
    /// <summary>Flags late active loans as Overdue.</summary>
    /// <param name="loans">Loans to check (from the current read).</param>
    private async Task RefreshOverdueAsync(IEnumerable<Loan> loans)
    {
        // Find active loans past due (domain IsOverdue checks status + date).
        var now = DateTime.UtcNow;
        var becameOverdue = loans.Where(l => l.IsOverdue(now)).ToList();
        // Flip each to Overdue via domain method.
        foreach (var loan in becameOverdue)
        {
            loan.MarkOverdue();
            _loans.Update(loan);
        }
        // Only hit the DB when something actually changed.
        if (becameOverdue.Count > 0)
            await _loans.SaveChangesAsync();
    }

    /// <summary>Counts active (not returned) loans for a member.</summary>
    /// <param name="memberId">Member ID.</param>
    /// <returns>Number of active loans.</returns>
    private async Task<int> CountActiveLoans(Guid memberId)
    {
        // Load all and filter in memory (generic repo has no query support).
        var all = await _loans.ListAsync();
        return all.Count(l => l.MemberId == memberId && l.Status == LoanStatus.Active);
    }

    // Bulk display data (barcode + book title + member name) to avoid a query per loan.
    /// <summary>Bulk-loads display data to avoid a query per loan.</summary>
    /// <returns>Barcodes, book titles, and member names keyed by ID.</returns>
    private async Task<(
        Dictionary<Guid, string> Barcodes,
        Dictionary<Guid, string> BookTitles,
        Dictionary<Guid, string> Names)> LookupDisplayData()
    {
        // Three list queries total, then build in-memory dictionaries.
        var copies = await _copies.ListAsync();
        var members = await _members.ListAsync();
        var books = await _books.ListAsync();
        // Map BookId -> Title first, so copies can resolve their title.
        var titlesByBookId = books.ToDictionary(b => b.Id, b => b.Title);
        return (
            copies.ToDictionary(c => c.Id, c => c.Barcode),
            copies.ToDictionary(
                c => c.Id,
                c => titlesByBookId.GetValueOrDefault(c.BookId, "Unknown title")),
            members.ToDictionary(m => m.Id, m => m.FullName));
    }

    /// <summary>Maps a Loan to its DTO using pre-loaded display data.</summary>
    /// <param name="l">Loan entity.</param>
    /// <param name="d">Bulk display dictionaries.</param>
    /// <returns>DTO for API responses.</returns>
    private static LoanDto ToDto(
        Loan l,
        (
            Dictionary<Guid, string> Barcodes,
            Dictionary<Guid, string> BookTitles,
            Dictionary<Guid, string> Names) d)
        => new(l.Id, l.BookCopyId, d.Barcodes.GetValueOrDefault(l.BookCopyId),
            d.BookTitles.GetValueOrDefault(l.BookCopyId),
            l.MemberId, d.Names.GetValueOrDefault(l.MemberId),
            l.BorrowedAt, l.DueDate, l.ReturnedAt, l.Status.ToString(), l.RenewalCount);
}
