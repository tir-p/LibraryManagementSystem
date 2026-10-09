// File: Category.cs
// Purpose: Defines the Category domain entity (a shelf section like "Science Fiction").
// Each Book belongs to one Category; a Category groups many Books.
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents a shelf section or genre (for example, "Science Fiction").
/// Groups many <see cref="Book"/> titles together.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// Shelf section like "Science Fiction". Name is unique (enforced in EF config).
public class Category : BaseEntity
{
    /// <summary>
    /// Gets the category name. Required, never blank, unique in the database.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Gets the optional description of what this category covers. May be null.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the books placed in this category.
    /// Navigation property used by EF Core for the one-to-many side.
    /// </summary>
    public ICollection<Book> Books { get; private set; } = new List<Book>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Category"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core re-hydration only; code uses the public ctor below.
    protected Category()
    {
        // Safe default so the non-nullable Name is never null after EF creates the object.
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Category"/> class.
    /// </summary>
    /// <param name="name">Category name. Cannot be empty.</param>
    /// <param name="description">Optional description.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty.</exception>
    public Category(string name, string? description = null)
    {
        // Guard: every shelf section needs a name; fail fast with a domain rule error.
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        // Trim removes accidental leading/trailing spaces from user input.
        Name = name.Trim();
        Description = description;
    }

    /// <summary>
    /// Updates the category name and description.
    /// </summary>
    /// <param name="name">New name. Cannot be empty.</param>
    /// <param name="description">New description. May be null to clear it.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty.</exception>
    public void Update(string name, string? description)
    {
        // Same guard as the constructor: name must stay valid on every update.
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Description = description;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }
}
