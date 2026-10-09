// File: Book.cs
// Purpose: Defines the Book domain entity (a catalog title such as "Dune").
// A Book is NOT a physical item - physical items are BookCopy.
// Each Book must link to one Author and one Category.
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents a catalog title in the library (for example, "Dune").
/// Physical, borrowable items for this title are tracked as <see cref="BookCopy"/>.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// A catalog title (e.g. "Dune"), NOT a physical item. Physical items are BookCopy.
// Private setters + ctor validation = no Book can exist without title, ISBN, author, category.
public class Book : BaseEntity
{
    /// <summary>
    /// Gets the title of the book. Required, never blank.
    /// </summary>
    public string Title { get; private set; }
    /// <summary>
    /// Gets the ISBN identifier of the book. Required, never blank.
    /// </summary>
    public string Isbn { get; private set; }
    /// <summary>
    /// Gets the optional description or summary. May be null.
    /// </summary>
    public string? Description { get; private set; }
    /// <summary>
    /// Gets the optional publisher name. May be null.
    /// </summary>
    public string? Publisher { get; private set; }
    /// <summary>
    /// Gets the language of the book. Defaults to "English" if not supplied.
    /// </summary>
    public string Language { get; private set; } = "English";
    /// <summary>
    /// Gets the optional URL of the cover image. May be null.
    /// </summary>
    public string? CoverImageUrl { get; private set; }

    /// <summary>
    /// Gets the year the book was published. 0 means unknown / not set.
    /// </summary>
    public int PublishedYear { get; private set; }
    /// <summary>
    /// Gets the number of pages. 0 means unknown / not set.
    /// </summary>
    public int Pages { get; private set; }

    /// <summary>
    /// Gets the foreign key of the author who wrote this book.
    /// </summary>
    public Guid AuthorId { get; private set; }
    /// <summary>
    /// Gets the navigation property to the author. Set by EF Core on load.
    /// </summary>
    public Author Author { get; private set; } = null!;

    /// <summary>
    /// Gets the foreign key of the category (shelf section) for this book.
    /// </summary>
    public Guid CategoryId { get; private set; }
    /// <summary>
    /// Gets the navigation property to the category. Set by EF Core on load.
    /// </summary>
    public Category Category { get; private set; } = null!;

    /// <summary>
    /// Gets the physical copies available for this title.
    /// Navigation property used by EF Core for the one-to-many side.
    /// </summary>
    public ICollection<BookCopy> Copies { get; private set; } = new List<BookCopy>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Book"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core re-hydration only.
    protected Book()
    {
        // Safe defaults so non-nullable strings are never null after EF creates the object.
        Title = string.Empty;
        Isbn = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Book"/> class.
    /// </summary>
    /// <param name="title">Book title. Cannot be empty.</param>
    /// <param name="isbn">ISBN identifier. Cannot be empty.</param>
    /// <param name="authorId">Id of the existing author. Cannot be empty GUID.</param>
    /// <param name="categoryId">Id of the existing category. Cannot be empty GUID.</param>
    /// <param name="publishedYear">Year published. 0 if unknown.</param>
    /// <param name="pages">Page count. 0 if unknown.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="publisher">Optional publisher.</param>
    /// <param name="language">Language name. Falls back to "English" when blank.</param>
    /// <param name="coverImageUrl">Optional cover image URL.</param>
    /// <exception cref="DomainException">Thrown when a required value is missing.</exception>
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
        // Guards: a catalog entry is useless without title, ISBN, author and category.
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title is required.");
        if (string.IsNullOrWhiteSpace(isbn))
            throw new DomainException("ISBN is required.");
        if (authorId == Guid.Empty)
            throw new DomainException("Author is required.");
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");

        // Trim user input to avoid hidden spaces; fall back to "English" for blank language.
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

    /// <summary>
    /// Updates the editable details of this book (title, description, publisher, year, pages, language, cover).
    /// Author and ISBN are intentionally not changed here.
    /// </summary>
    /// <param name="title">New title. Cannot be empty.</param>
    /// <param name="description">New description. May be null.</param>
    /// <param name="publisher">New publisher. May be null.</param>
    /// <param name="publishedYear">New published year.</param>
    /// <param name="pages">New page count.</param>
    /// <param name="language">New language. Falls back to "English" when blank.</param>
    /// <param name="coverImageUrl">New cover image URL. May be null.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="title"/> is empty.</exception>
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
        // Guard: title must stay valid on every edit.
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title is required.");

        Title = title.Trim();
        Description = description;
        Publisher = publisher;
        PublishedYear = publishedYear;
        Pages = pages;
        Language = string.IsNullOrWhiteSpace(language) ? "English" : language.Trim();
        CoverImageUrl = coverImageUrl;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }
}
