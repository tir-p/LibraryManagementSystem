using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

public interface IBookService
{
    Task<IReadOnlyList<BookDto>> GetAllAsync();
    Task<BookDto> GetByIdAsync(Guid id);
    Task<BookDto> CreateAsync(CreateBookRequest request);
    Task<BookDto> UpdateAsync(Guid id, UpdateBookRequest request);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<BookCopyDto>> GetCopiesAsync(Guid bookId);
    Task<BookCopyDto> AddCopyAsync(Guid bookId, CreateBookCopyRequest request);
    Task<IReadOnlyList<BookAvailabilityDto>> GetAvailabilityAsync();
}
