using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// A catalog title (e.g. "Dune"), NOT a physical item. Physical items are BookCopy.
// Private setters + ctor validation = no Book can exist without title, ISBN, author, category.
public class Book : BaseEntity
{
    public string Title { get; private set; }
    public string Isbn { get; private set; }
    public string? Description { get; private set; }
    public string? Publisher { get; private set; }
    public string Language { get; private set; } = "English";
    public string? CoverImageUrl { get; private set; }

    public int PublishedYear { get; private set; }
    public int Pages { get; private set; }

    public Guid AuthorId { get; private set; }
    public Author Author { get; private set; } = null!;

    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    public ICollection<BookCopy> Copies { get; private set; } = new List<BookCopy>();

    // EF Core re-hydration only.
    protected Book()
    {
        Title = string.Empty;
        Isbn = string.Empty;
    }

    public Book(
        string title,
        string isbn,
        Guid authorId,
        Guid categoryId,
        int publishedYear = 0,
        int pages = 0,
        string? description = null,
        string? publisher = null,
        string language = "English",
        string? coverImageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title is required.");
        if (string.IsNullOrWhiteSpace(isbn))
            throw new DomainException("ISBN is required.");
        if (authorId == Guid.Empty)
            throw new DomainException("Author is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");

        Title = title.Trim();
        Isbn = isbn.Trim();
        AuthorId = authorId;
        CategoryId = categoryId;
        PublishedYear = publishedYear;
        Pages = pages;
        Description = description;
        Publisher = publisher;
        Language = string.IsNullOrWhiteSpace(language) ? "English" : language.Trim();
        CoverImageUrl = coverImageUrl;
    }

    // Edits go through here so Title can never be blanked and UpdatedAt is bumped.
    public void UpdateDetails(
        string title,
        string? description,
        string? publisher,
        int publishedYear,
        int pages,
        string language,
        string? coverImageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title is required.");

        Title = title.Trim();
        Description = description;
        Publisher = publisher;
        PublishedYear = publishedYear;
        Pages = pages;
        Language = string.IsNullOrWhiteSpace(language) ? "English" : language.Trim();
        CoverImageUrl = coverImageUrl;
        Touch();
    }
}
