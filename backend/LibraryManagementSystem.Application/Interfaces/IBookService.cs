// -----------------------------------------------------------------------------
// File header:
// IBookService is the use-case contract for books and their physical copies.
// It covers title CRUD plus copy management (list/add) and availability counts.
// Controllers depend on this interface for testability (mockable).
// -----------------------------------------------------------------------------
using LibraryManagementSystem.Application.DTOs;

namespace LibraryManagementSystem.Application.Interfaces;

/// <summary>
/// Book and BookCopy use cases.
/// </summary>
public interface IBookService
{
    /// <summary>Gets all book titles.</summary>
    /// <returns>Read-only list of books.</returns>
    Task<IReadOnlyList<BookDto>> GetAllAsync();
    /// <summary>Gets one book by ID.</summary>
    /// <param name="id">Book ID.</param>
    /// <returns>The matching book.</returns>
    Task<BookDto> GetByIdAsync(Guid id);
    /// <summary>Creates a new book title (validates author/category + unique ISBN).</summary>
    /// <param name="request">Title, ISBN, author/category IDs, details.</param>
    /// <returns>The created book.</returns>
    Task<BookDto> CreateAsync(CreateBookRequest request);
    /// <summary>Updates details of an existing book.</summary>
    /// <param name="id">Book ID.</param>
    /// <param name="request">New title/description/etc.</param>
    /// <returns>The updated book.</returns>
    Task<BookDto> UpdateAsync(Guid id, UpdateBookRequest request);
    /// <summary>Deletes a book title.</summary>
    /// <param name="id">Book ID.</param>
    Task DeleteAsync(Guid id);
    /// <summary>Gets all physical copies of one book.</summary>
    /// <param name="bookId">Parent book ID.</param>
    /// <returns>Copies belonging to that book.</returns>
    Task<IReadOnlyList<BookCopyDto>> GetCopiesAsync(Guid bookId);
    /// <summary>Adds a physical copy (validates unique barcode).</summary>
    /// <param name="bookId">Parent book ID.</param>
    /// <param name="request">Barcode + shelf location.</param>
    /// <returns>The created copy.</returns>
    Task<BookCopyDto> AddCopyAsync(Guid bookId, CreateBookCopyRequest request);
    /// <summary>Gets total/available copy counts per title.</summary>
    /// <returns>One row per book title.</returns>
    Task<IReadOnlyList<BookAvailabilityDto>> GetAvailabilityAsync();
}
