using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Enums;
using LibraryManagementSystem.Domain.Exceptions;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// The heart of the system: borrow / return / renew.
// Rules live in the entities (BookCopy.MarkLoaned, Loan.Return/Renew, Member.CanBorrow);
// this service only loads entities, calls those methods, and saves ONCE,
// so Loan + BookCopy changes commit atomically via the shared DbContext.
public class LoanService : ILoanService
{
    private readonly IRepository<Loan> _loans;
    private readonly IRepository<BookCopy> _copies;
    private readonly IRepository<Member> _members;

    public LoanService(
        IRepository<Loan> loans,
        IRepository<BookCopy> copies,
        IRepository<Member> members)
    {
        _loans = loans;
        _copies = copies;
        _members = members;
    }

    public async Task<IReadOnlyList<LoanDto>> GetAllAsync()
    {
        var loans = await _loans.ListAsync();
        var lookups = await LookupDisplayData();
        return loans.Select(l => ToDto(l, lookups)).ToList();
    }

    public async Task<LoanDto> GetByIdAsync(Guid id)
        => ToDto(await GetLoanOrThrow(id), await LookupDisplayData());

    public async Task<LoanDto> BorrowAsync(BorrowRequest request)
    {
        var member = await _members.GetByIdAsync(request.MemberId)
            ?? throw new KeyNotFoundException($"Member {request.MemberId} not found.");
        var copy = await _copies.GetByIdAsync(request.BookCopyId)
            ?? throw new KeyNotFoundException($"Book copy {request.BookCopyId} not found.");

        // Domain guards: available copy + member allowed to borrow (active, under cap).
        if (!copy.IsAvailable)
            throw new DomainException("Copy is not available for loan.");

        var activeCount = await CountActiveLoans(member.Id);
        if (!member.CanBorrow(activeCount))
            throw new DomainException("Member cannot borrow more books.");

        var loan = new Loan(copy.Id, member.Id, DateTime.UtcNow, request.LoanDays);
        copy.MarkLoaned();

        await _loans.AddAsync(loan);
        _copies.Update(copy);
        await _loans.SaveChangesAsync();

        return ToDto(loan, await LookupDisplayData());
    }

    public async Task<LoanDto> ReturnAsync(Guid id)
    {
        var loan = await GetLoanOrThrow(id);
        var copy = await _copies.GetByIdAsync(loan.BookCopyId)
            ?? throw new KeyNotFoundException($"Book copy {loan.BookCopyId} not found.");

        loan.Return();
        copy.MarkAvailable();

        _loans.Update(loan);
        _copies.Update(copy);
        await _loans.SaveChangesAsync();

        return ToDto(loan, await LookupDisplayData());
    }

    public async Task<LoanDto> RenewAsync(Guid id)
    {
        var loan = await GetLoanOrThrow(id);
        loan.Renew(); // Max 2 renewals enforced inside.
        _loans.Update(loan);
        await _loans.SaveChangesAsync();
        return ToDto(loan, await LookupDisplayData());
    }

    private async Task<Loan> GetLoanOrThrow(Guid id)
        => await _loans.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Loan {id} not found.");

    private async Task<int> CountActiveLoans(Guid memberId)
    {
        var all = await _loans.ListAsync();
        return all.Count(l => l.MemberId == memberId && l.Status == LoanStatus.Active);
    }

    // Bulk display data (barcode + member name) to avoid a query per loan.
    private async Task<(Dictionary<Guid, string> Barcodes, Dictionary<Guid, string> Names)> LookupDisplayData()
    {
        var copies = await _copies.ListAsync();
        var members = await _members.ListAsync();
        return (
            copies.ToDictionary(c => c.Id, c => c.Barcode),
            members.ToDictionary(m => m.Id, m => m.FullName));
    }

    private static LoanDto ToDto(
        Loan l,
        (Dictionary<Guid, string> Barcodes, Dictionary<Guid, string> Names) d)
        => new(l.Id, l.BookCopyId, d.Barcodes.GetValueOrDefault(l.BookCopyId),
            l.MemberId, d.Names.GetValueOrDefault(l.MemberId),
            l.BorrowedAt, l.DueDate, l.ReturnedAt, l.Status.ToString(), l.RenewalCount);
}
