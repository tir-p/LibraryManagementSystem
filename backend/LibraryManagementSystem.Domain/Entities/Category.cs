using LibraryManagementSystem.Domain.Exceptions;

namespace LibraryManagementSystem.Domain.Entities;

// Shelf section like "Science Fiction". Name is unique (enforced in EF config).
public class Category : BaseEntity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public ICollection<Book> Books { get; private set; } = new List<Book>();

    // EF Core re-hydration only; code uses the public ctor below.
    protected Category()
    {
        Name = string.Empty;
    }

    public Category(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Description = description;
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        Name = name.Trim();
        Description = description;
        Touch();
    }
}
