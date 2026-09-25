using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

public interface ILoanService
{
    Task<IReadOnlyList<LoanDto>> GetAllAsync();
    Task<LoanDto> GetByIdAsync(Guid id);
    Task<LoanDto> BorrowAsync(BorrowRequest request);
    Task<LoanDto> ReturnAsync(Guid id);
    Task<LoanDto> RenewAsync(Guid id);
}
