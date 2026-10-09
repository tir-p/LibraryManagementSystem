using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Exceptions;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// -----------------------------------------------------------------------------
// File header:
// BookService implements book + copy use cases: title CRUD, copy list/add,
// and availability counts. Author/Category names are looked up in bulk (two
// extra queries) so the generic repo stays free of EF Includes. Duplicate
// ISBN/barcode gives a friendly 400 via DomainException; missing rows -> 404.
// -----------------------------------------------------------------------------
// Books plus their physical copies. Author/Category names are looked up in bulk
// (two extra queries) so the generic repository stays free of EF Includes.
/// <summary>
/// Book and BookCopy use cases (titles + physical copies).
/// </summary>
public class BookService : IBookService
{
    // Repositories: books (titles), authors/categories (for validation + names), copies.
    private readonly IRepository<Book> _books;
    private readonly IRepository<Author> _authors;
    private readonly IRepository<Category> _categories;
    private readonly IRepository<BookCopy> _copies;

    /// <summary>
    /// Initializes the service with all repositories it needs (injected by DI).
    /// </summary>
    /// <param name="books">Book repository.</param>
    /// <param name="authors">Author repository (validation + display names).</param>
    /// <param name="categories">Category repository (validation + display names).</param>
    /// <param name="copies">BookCopy repository.</param>
    public BookService(
        IRepository<Book> books,
        IRepository<Author> authors,
        IRepository<Category> categories,
        IRepository<BookCopy> copies)
    {
        _books = books;
        _authors = authors;
        _categories = categories;
        _copies = copies;
    }

    /// <summary>Gets all book titles with author/category names.</summary>
    /// <returns>All books as DTOs.</returns>
    public async Task<IReadOnlyList<BookDto>> GetAllAsync()
    {
        // Load titles, then bulk-load display names once (avoids query per book).
        var books = await _books.ListAsync();
        var names = await LookupNames();
        // Map each book, resolving names from the dictionary.
        return books.Select(b => ToDto(b, names)).ToList();
    }

    /// <summary>Gets one book by ID.</summary>
    /// <param name="id">Book ID.</param>
    /// <returns>The matching book DTO.</returns>
    public async Task<BookDto> GetByIdAsync(Guid id)
    {
        // 404 if missing, else map with resolved names.
        var book = await GetBookOrThrow(id);
        return ToDto(book, await LookupNames());
    }

    /// <summary>Creates a new book title.</summary>
    /// <param name="request">Title, ISBN, author/category IDs, details.</param>
    /// <returns>The created book DTO.</returns>
    public async Task<BookDto> CreateAsync(CreateBookRequest request)
    {
        // Fail fast with 404s instead of raw FK violations from the DB.
        await GetAuthorOrThrow(request.AuthorId);
        await GetCategoryOrThrow(request.CategoryId);

        // Friendly 400 instead of a raw SQL unique-violation 500 on duplicate ISBN.
        var existing = await _books.ListAsync();
        if (existing.Any(b => b.Isbn.Equals(request.Isbn.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A book with this ISBN already exists.");

        // Domain constructor validates (blank title, bad ISBN, etc.).
        var book = new Book(
            request.Title, request.Isbn, request.AuthorId, request.CategoryId,
            request.PublishedYear, request.Pages, request.Description,
            request.Publisher, request.Language, request.CoverImageUrl);

        // Persist and return with display names.
        await _books.AddAsync(book);
        await _books.SaveChangesAsync();
        return ToDto(book, await LookupNames());
    }

    /// <summary>Updates details of an existing book.</summary>
    /// <param name="id">Book ID.</param>
    /// <param name="request">New title/description/etc.</param>
    /// <returns>The updated book DTO.</returns>
    public async Task<BookDto> UpdateAsync(Guid id, UpdateBookRequest request)
    {
        // Load or 404, apply domain update (validates fields), save.
        var book = await GetBookOrThrow(id);
        book.UpdateDetails(
            request.Title, request.Description, request.Publisher,
            request.PublishedYear, request.Pages, request.Language, request.CoverImageUrl);
        _books.Update(book);
        await _books.SaveChangesAsync();
        return ToDto(book, await LookupNames());
    }

    /// <summary>Deletes a book title.</summary>
    /// <param name="id">Book ID.</param>
    public async Task DeleteAsync(Guid id)
    {
        // Load or 404, then remove and save.
        var book = await GetBookOrThrow(id);
        _books.Remove(book);
        await _books.SaveChangesAsync();
    }

    /// <summary>Gets all physical copies of one book.</summary>
    /// <param name="bookId">Parent book ID.</param>
    /// <returns>Copies with parent title filled in.</returns>
    public async Task<IReadOnlyList<BookCopyDto>> GetCopiesAsync(Guid bookId)
    {
        // Ensure the parent book exists (404 otherwise).
        var book = await GetBookOrThrow(bookId);
        // Generic repo has no filter, so load all and filter in memory.
        var copies = await _copies.ListAsync();
        return copies
            .Where(c => c.BookId == bookId)
            .Select(c => ToCopyDto(c, book.Title))
            .ToList();
    }

    /// <summary>Adds a physical copy to a book.</summary>
    /// <param name="bookId">Parent book ID.</param>
    /// <param name="request">Barcode + shelf location.</param>
    /// <returns>The created copy DTO.</returns>
    public async Task<BookCopyDto> AddCopyAsync(Guid bookId, CreateBookCopyRequest request)
    {
        // Parent must exist.
        var book = await GetBookOrThrow(bookId);
        // Barcode must be globally unique -> friendly 400 (trim + case-insensitive).
        var copies = await _copies.ListAsync();
        if (copies.Any(c => c.Barcode.Equals(request.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("A copy with this barcode already exists.");

        // Constructor validates barcode; new copies start as Available.
        var copy = new BookCopy(book.Id, request.Barcode, request.ShelfLocation);
        await _copies.AddAsync(copy);
        await _copies.SaveChangesAsync();
        return ToCopyDto(copy, book.Title);
    }

    /// <summary>Gets total/available copy counts per title.</summary>
    /// <returns>One availability row per book.</returns>
    public async Task<IReadOnlyList<BookAvailabilityDto>> GetAvailabilityAsync()
    {
        // Load both lists once, then count in memory (no N+1 queries).
        var books = await _books.ListAsync();
        var copies = await _copies.ListAsync();
        return books
            .Select(b => new BookAvailabilityDto(
                b.Id,
                copies.Count(c => c.BookId == b.Id),
                copies.Count(c => c.BookId == b.Id && c.IsAvailable)))
            .ToList();
    }

    // One bulk fetch of display names (avoids a query per book).
    /// <summary>Bulk-loads author/category names keyed by ID.</summary>
    /// <returns>Dictionary of ID -> display name.</returns>
    private async Task<Dictionary<Guid, string>> LookupNames()
    {
        // Two queries total, then merge into one lookup for ToDto.
        var authors = await _authors.ListAsync();
        var categories = await _categories.ListAsync();
        var names = new Dictionary<Guid, string>();
        foreach (var a in authors) names[a.Id] = a.Name;
        foreach (var c in categories) names[c.Id] = c.Name;
        return names;
    }

    /// <summary>Loads a book or throws 404.</summary>
    /// <param name="id">Book ID.</param>
    /// <returns>The book entity.</returns>
    private async Task<Book> GetBookOrThrow(Guid id)
        => await _books.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Book {id} not found.");

    /// <summary>Ensures an author exists (for FK validation).</summary>
    /// <param name="id">Author ID.</param>
    private async Task GetAuthorOrThrow(Guid id)
    {
        if (await _authors.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"Author {id} not found.");
    }

    /// <summary>Ensures a category exists (for FK validation).</summary>
    /// <param name="id">Category ID.</param>
    private async Task GetCategoryOrThrow(Guid id)
    {
        if (await _categories.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"Category {id} not found.");
    }

    /// <summary>Maps a Book to its DTO, resolving names.</summary>
    /// <param name="b">Domain entity.</param>
    /// <param name="names">ID -> name lookup.</param>
    /// <returns>DTO for API responses.</returns>
    private static BookDto ToDto(Book b, Dictionary<Guid, string> names)
        => new(b.Id, b.Title, b.Isbn, b.Description, b.Publisher, b.Language,
            b.PublishedYear, b.Pages, b.CoverImageUrl,
            b.AuthorId, names.GetValueOrDefault(b.AuthorId),
            b.CategoryId, names.GetValueOrDefault(b.CategoryId));

    /// <summary>Maps a BookCopy to its DTO.</summary>
    /// <param name="c">Copy entity.</param>
    /// <param name="bookTitle">Parent title for display.</param>
    /// <returns>DTO for API responses.</returns>
    private static BookCopyDto ToCopyDto(BookCopy c, string? bookTitle)
        => new(c.Id, c.BookId, bookTitle, c.Barcode,
            c.ShelfLocation, c.Status.ToString(), c.IsAvailable);
}
