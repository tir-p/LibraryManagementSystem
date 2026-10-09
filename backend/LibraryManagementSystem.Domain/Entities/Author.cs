// File: Author.cs
// Purpose: Defines the Author domain entity (a writer in the library catalog).
// An Author owns many Books (one-to-many). Creation and updates go through
// constructors/methods so the Name can never be empty.
using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

/// <summary>
/// Represents a writer whose books are held by the library.
/// Inherits identity and audit fields from <see cref="BaseEntity"/>.
/// </summary>
// Catalog entry for a writer. Holds its Books so EF can map the one-to-many side.
public class Author : BaseEntity
{
    /// <summary>
    /// Gets the author's full name. Required, never blank.
    /// </summary>
    public string Name { get; private set; }
    /// <summary>
    /// Gets the optional short biography of the author. May be null.
    /// </summary>
    public string? Biography { get; private set; }
    /// <summary>
    /// Gets the optional date of birth of the author. May be null if unknown.
    /// </summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>
    /// Gets the books written by this author.
    /// Navigation property used by EF Core for the one-to-many side.
    /// </summary>
    public ICollection<Book> Books { get; private set; } = new List<Book>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Author"/> class for EF Core.
    /// Required so EF Core can re-create the object when reading from the database.
    /// Application code should use the public constructor instead.
    /// </summary>
    // EF Core needs a parameterless ctor to re-hydrate rows; code never calls it.
    protected Author()
    {
        // Provide a safe default so the non-nullable Name is never null after EF creates the object.
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Author"/> class.
    /// </summary>
    /// <param name="name">The author's full name. Cannot be empty or whitespace.</param>
    /// <param name="biography">Optional biography text.</param>
    /// <param name="dateOfBirth">Optional date of birth.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty.</exception>
    // Creation goes through here so required data + validation can't be skipped.
    public Author(string name, string? biography = null, DateOnly? dateOfBirth = null)
    {
        // Guard: every author must have a name; fail fast with a domain rule error.
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name is required.");

        // Trim removes accidental leading/trailing spaces from user input.
        Name = name.Trim();
        Biography = biography;
        DateOfBirth = dateOfBirth;
    }

    /// <summary>
    /// Updates the author's name and biography.
    /// </summary>
    /// <param name="name">The new full name. Cannot be empty or whitespace.</param>
    /// <param name="biography">The new biography text. May be null to clear it.</param>
    /// <exception cref="DomainException">Thrown when <paramref name="name"/> is empty.</exception>
    public void Update(string name, string? biography)
    {
        // Same guard as the constructor: name must stay valid on every update.
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name is required.");

        Name = name.Trim();
        Biography = biography;
        // Refresh UpdatedAt so we know when this row last changed.
        Touch();
    }
}
