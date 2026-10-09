// -----------------------------------------------------------------------------
// File header:
// BookDtos defines book shapes: BookDto (title + author/category names for
// display), Create/Update requests (IDs only, no names), BookCopyDto (one
// physical copy), and BookAvailabilityDto (copy counts per title).
// For junior devs: requests carry IDs; the service resolves names via lookup.
// -----------------------------------------------------------------------------
namespace LibraryManagementSystem.Application.DTOs;

// BookDto carries Author/Category names for display; requests carry only IDs.
// Names are resolved in the service via separate lookups (keeps the generic repo simple).
/// <summary>
/// Book data returned to clients, with denormalized names for display.
/// </summary>
/// <param name="Id">Unique book (title) identifier.</param>
/// <param name="Title">Book title.</param>
/// <param name="Isbn">Unique ISBN code.</param>
/// <param name="Description">Optional synopsis.</param>
/// <param name="Publisher">Optional publisher name.</param>
/// <param name="Language">Language name (defaults to English).</param>
/// <param name="PublishedYear">Year of publication.</param>
/// <param name="Pages">Page count.</param>
/// <param name="CoverImageUrl">Optional cover image URL.</param>
/// <param name="AuthorId">FK to the author.</param>
/// <param name="AuthorName">Author display name (resolved by the service).</param>
/// <param name="CategoryId">FK to the category.</param>
/// <param name="CategoryName">Category display name (resolved by the service).</param>
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

/// <summary>
/// Payload for creating a new book title.
/// </summary>
/// <param name="Title">Required title.</param>
/// <param name="Isbn">Required unique ISBN.</param>
/// <param name="AuthorId">Existing author ID (validated in service).</param>
/// <param name="CategoryId">Existing category ID (validated in service).</param>
/// <param name="PublishedYear">Publication year (0 = unknown).</param>
/// <param name="Pages">Page count.</param>
/// <param name="Description">Optional synopsis.</param>
/// <param name="Publisher">Optional publisher.</param>
/// <param name="Language">Language name.</param>
/// <param name="CoverImageUrl">Optional cover URL.</param>
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

/// <summary>
/// Payload for updating book details (author/category/ISBN are immutable here).
/// </summary>
/// <param name="Title">Updated title.</param>
/// <param name="Description">Updated description.</param>
/// <param name="Publisher">Updated publisher.</param>
/// <param name="PublishedYear">Updated year.</param>
/// <param name="Pages">Updated page count.</param>
/// <param name="Language">Updated language.</param>
/// <param name="CoverImageUrl">Updated cover URL.</param>
public record UpdateBookRequest(
    string Title,
    string? Description,
    string? Publisher,
    int PublishedYear,
    int Pages,
    string Language,
    string? CoverImageUrl);

// Physical copy of a book. Status is a string ("Available", "Loaned"...) matching the enum.
/// <summary>
/// One physical copy that can be borrowed.
/// </summary>
/// <param name="Id">Unique copy identifier.</param>
/// <param name="BookId">Parent book (title) ID.</param>
/// <param name="BookTitle">Parent title for display.</param>
/// <param name="Barcode">Unique barcode sticker on the copy.</param>
/// <param name="ShelfLocation">Optional shelf code (e.g. "A-3-12").</param>
/// <param name="Status">Copy status string (Available, Loaned, ...).</param>
/// <param name="IsAvailable">True when the copy can be borrowed.</param>
public record BookCopyDto(
    Guid Id,
    Guid BookId,
    string? BookTitle,
    string Barcode,
    string? ShelfLocation,
    string Status,
    bool IsAvailable);

/// <summary>
/// Payload for adding a physical copy to a book.
/// </summary>
/// <param name="Barcode">Required unique barcode.</param>
/// <param name="ShelfLocation">Optional shelf location.</param>
public record CreateBookCopyRequest(string Barcode, string? ShelfLocation);

// One row per title: avoids N+1 fetches of copies per book from the frontend.
/// <summary>
/// Copy counts for one title (avoids fetching all copies per book).
/// </summary>
/// <param name="BookId">Book (title) ID.</param>
/// <param name="TotalCopies">Total physical copies owned.</param>
/// <param name="AvailableCopies">Copies currently available to borrow.</param>
public record BookAvailabilityDto(Guid BookId, int TotalCopies, int AvailableCopies);
