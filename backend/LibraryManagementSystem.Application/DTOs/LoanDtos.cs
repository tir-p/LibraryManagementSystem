// -----------------------------------------------------------------------------
// File header:
// LoanDtos defines loan shapes: LoanDto is a borrowing record with display
// names (barcode, book title, member name), BorrowRequest is what the client
// sends to check out a copy. Status: Active, Returned, Overdue.
// For junior devs: a Loan links one BookCopy to one Member with dates.
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

/// <summary>
/// Loan record returned to clients, with display names resolved by the service.
/// </summary>
/// <param name="Id">Unique loan identifier.</param>
/// <param name="BookCopyId">Borrowed physical copy ID.</param>
/// <param name="Barcode">Copy barcode for display.</param>
/// <param name="BookTitle">Book title for display.</param>
/// <param name="MemberId">Borrowing member ID.</param>
/// <param name="MemberName">Member full name for display.</param>
/// <param name="BorrowedAt">UTC time of checkout.</param>
/// <param name="DueDate">UTC due date (BorrowedAt + loan days).</param>
/// <param name="ReturnedAt">UTC return time, null while still out.</param>
/// <param name="Status">Loan status string (Active, Overdue, Returned).</param>
/// <param name="RenewalCount">How many times the loan was renewed (max 2).</param>
public record LoanDto(
    Guid Id,
    Guid BookCopyId,
    string? Barcode,
    string? BookTitle,
    Guid MemberId,
    string? MemberName,
    DateTime BorrowedAt,
    DateTime DueDate,
    DateTime? ReturnedAt,
    string Status,
    int RenewalCount);

/// <summary>
/// Payload for borrowing a copy.
/// </summary>
/// <param name="BookCopyId">Available copy to borrow.</param>
/// <param name="MemberId">Active member who borrows.</param>
/// <param name="LoanDays">Loan period in days (default 14).</param>
public record BorrowRequest(Guid BookCopyId, Guid MemberId, int LoanDays = 14);
