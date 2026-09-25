namespace LibraryManagementSystem.Application.DTOs;

public record LoanDto(
    Guid Id,
    Guid BookCopyId,
    string? Barcode,
    Guid MemberId,
    string? MemberName,
    DateTime BorrowedAt,
    DateTime DueDate,
    DateTime? ReturnedAt,
    string Status,
    int RenewalCount);

public record BorrowRequest(Guid BookCopyId, Guid MemberId, int LoanDays = 14);
