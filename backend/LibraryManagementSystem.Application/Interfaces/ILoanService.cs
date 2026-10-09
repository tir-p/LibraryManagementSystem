// -----------------------------------------------------------------------------
// File header:
// ILoanService is the use-case contract for borrowing: list/get loans plus
// borrow, return, and renew. Business rules live in the domain entities;
// this interface just exposes the operations to controllers.
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

/// <summary>
/// Loan use cases (borrow / return / renew).
/// </summary>
public interface ILoanService
{
    /// <summary>Gets all loans (refreshing overdue flags on read).</summary>
    /// <returns>Read-only list of loans.</returns>
    Task<IReadOnlyList<LoanDto>> GetAllAsync();
    /// <summary>Gets one loan by ID.</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The matching loan.</returns>
    Task<LoanDto> GetByIdAsync(Guid id);
    /// <summary>Borrows an available copy for an eligible member.</summary>
    /// <param name="request">Copy ID, member ID, loan days.</param>
    /// <returns>The created active loan.</returns>
    Task<LoanDto> BorrowAsync(BorrowRequest request);
    /// <summary>Returns a borrowed copy (marks loan Returned, copy Available).</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The updated loan.</returns>
    Task<LoanDto> ReturnAsync(Guid id);
    /// <summary>Renews a loan (max 2 renewals, enforced in domain).</summary>
    /// <param name="id">Loan ID.</param>
    /// <returns>The renewed loan with extended due date.</returns>
    Task<LoanDto> RenewAsync(Guid id);
}
