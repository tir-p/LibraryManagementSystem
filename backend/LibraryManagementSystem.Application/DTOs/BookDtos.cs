namespace LibraryManagementSystem.Application.DTOs;

// BookDto carries Author/Category names for display; requests carry only IDs.
// Names are resolved in the service via separate lookups (keeps the generic repo simple).
public record BookDto(
    Guid Id,
    string Title,
    string Isbn,
    string? Description,
    string? Publisher,
    string Language,
    int PublishedYear,
    int Pages,
    string? CoverImageUrl,
    Guid AuthorId,
    string? AuthorName,
    Guid CategoryId,
    string? CategoryName);

public record CreateBookRequest(
    string Title,
    string Isbn,
    Guid AuthorId,
    Guid CategoryId,
    int PublishedYear = 0,
    int Pages = 0,
    string? Description = null,
    string? Publisher = null,
    string Language = "English",
    string? CoverImageUrl = null);

public record UpdateBookRequest(
    string Title,
    string? Description,
    string? Publisher,
    int PublishedYear,
    int Pages,
    string Language,
    string? CoverImageUrl);

// Physical copy of a book. Status is a string ("Available", "Loaned"...) matching the enum.
public record BookCopyDto(
    Guid Id,
    Guid BookId,
    string? BookTitle,
    string Barcode,
    string? ShelfLocation,
    string Status,
    bool IsAvailable);

public record CreateBookCopyRequest(string Barcode, string? ShelfLocation);
