using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// Catalog entry for a writer. Holds its Books so EF can map the one-to-many side.
public class Author : BaseEntity
{
    public string Name { get; private set; }
    public string? Biography { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }

    public ICollection<Book> Books { get; private set; } = new List<Book>();

    // EF Core needs a parameterless ctor to re-hydrate rows; code never calls it.
    protected Author()
    {
        Name = string.Empty;
    }

    // Creation goes through here so required data + validation can't be skipped.
    public Author(string name, string? biography = null, DateOnly? dateOfBirth = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name is required.");

        Name = name.Trim();
        Biography = biography;
        DateOfBirth = dateOfBirth;
    }

    public void Update(string name, string? biography)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name is required.");

        Name = name.Trim();
        Biography = biography;
        Touch();
    }
}
