using LibraryManagementSystem.Application.DTOs;
using LibraryManagementSystem.Application.Interfaces;
using LibraryManagementSystem.Domain.Entities;
using LibraryManagementSystem.Domain.Interfaces;

namespace LibraryManagementSystem.Application.Services;

// Books plus their physical copies. Author/Category names are looked up in bulk
// (two extra queries) so the generic repository stays free of EF Includes.
public class BookService : IBookService
{
    private readonly IRepository<Book> _books;
    private readonly IRepository<Author> _authors;
    private readonly IRepository<Category> _categories;
    private readonly IRepository<BookCopy> _copies;

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

    public async Task<IReadOnlyList<BookDto>> GetAllAsync()
    {
        var books = await _books.ListAsync();
        var names = await LookupNames();
        return books.Select(b => ToDto(b, names)).ToList();
    }

    public async Task<BookDto> GetByIdAsync(Guid id)
    {
        var book = await GetBookOrThrow(id);
        return ToDto(book, await LookupNames());
    }

    public async Task<BookDto> CreateAsync(CreateBookRequest request)
    {
        // Fail fast with 404s instead of raw FK violations from the DB.
        await GetAuthorOrThrow(request.AuthorId);
        await GetCategoryOrThrow(request.CategoryId);

        var book = new Book(
            request.Title, request.Isbn, request.AuthorId, request.CategoryId,
            request.PublishedYear, request.Pages, request.Description,
            request.Publisher, request.Language, request.CoverImageUrl);

        await _books.AddAsync(book);
        await _books.SaveChangesAsync();
        return ToDto(book, await LookupNames());
    }

    public async Task<BookDto> UpdateAsync(Guid id, UpdateBookRequest request)
    {
        var book = await GetBookOrThrow(id);
        book.UpdateDetails(
            request.Title, request.Description, request.Publisher,
            request.PublishedYear, request.Pages, request.Language, request.CoverImageUrl);
        _books.Update(book);
        await _books.SaveChangesAsync();
        return ToDto(book, await LookupNames());
    }

    public async Task DeleteAsync(Guid id)
    {
        var book = await GetBookOrThrow(id);
        _books.Remove(book);
        await _books.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<BookCopyDto>> GetCopiesAsync(Guid bookId)
    {
        var book = await GetBookOrThrow(bookId);
        var copies = await _copies.ListAsync();
        return copies
            .Where(c => c.BookId == bookId)
            .Select(c => ToCopyDto(c, book.Title))
            .ToList();
    }

    public async Task<BookCopyDto> AddCopyAsync(Guid bookId, CreateBookCopyRequest request)
    {
        var book = await GetBookOrThrow(bookId);
        var copy = new BookCopy(book.Id, request.Barcode, request.ShelfLocation);
        await _copies.AddAsync(copy);
        await _copies.SaveChangesAsync();
        return ToCopyDto(copy, book.Title);
    }

    // One bulk fetch of display names (avoids a query per book).
    private async Task<Dictionary<Guid, string>> LookupNames()
    {
        var authors = await _authors.ListAsync();
        var categories = await _categories.ListAsync();
        var names = new Dictionary<Guid, string>();
        foreach (var a in authors) names[a.Id] = a.Name;
        foreach (var c in categories) names[c.Id] = c.Name;
        return names;
    }

    private async Task<Book> GetBookOrThrow(Guid id)
        => await _books.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Book {id} not found.");

    private async Task GetAuthorOrThrow(Guid id)
    {
        if (await _authors.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"Author {id} not found.");
    }

    private async Task GetCategoryOrThrow(Guid id)
    {
        if (await _categories.GetByIdAsync(id) is null)
            throw new KeyNotFoundException($"Category {id} not found.");
    }

    private static BookDto ToDto(Book b, Dictionary<Guid, string> names)
        => new(b.Id, b.Title, b.Isbn, b.Description, b.Publisher, b.Language,
            b.PublishedYear, b.Pages, b.CoverImageUrl,
            b.AuthorId, names.GetValueOrDefault(b.AuthorId),
            b.CategoryId, names.GetValueOrDefault(b.CategoryId));

    private static BookCopyDto ToCopyDto(BookCopy c, string? bookTitle)
        => new(c.Id, c.BookId, bookTitle, c.Barcode,
            c.ShelfLocation, c.Status.ToString(), c.IsAvailable);
}
