using LibraryManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Infrastructure.Persistence;

// Seeds the catalog with programming books + Atomic Habits.
// Runs on startup (see Program.cs). Seed-only-if-empty so restarts
// never wipe user data (loans, added books/copies are preserved).
public static class DataSeeder
{
    public static async Task SeedAsync(LibraryDbContext db)
    {
        if (await db.Books.AnyAsync())
            return;

        var programming = await GetOrCreateCategory(db, "Programming", "Software development books");
        var selfHelp = await GetOrCreateCategory(db, "Self-Help", "Productivity and personal growth");

        var martin = await GetOrCreateAuthor(db, "Robert C. Martin");
        var thomas = await GetOrCreateAuthor(db, "David Thomas");
        var gamma = await GetOrCreateAuthor(db, "Erich Gamma");
        var fowler = await GetOrCreateAuthor(db, "Martin Fowler");
        var clear = await GetOrCreateAuthor(db, "James Clear");

        var books = new List<Book>
        {
            new(
                "Clean Code: A Handbook of Agile Software Craftsmanship",
                "9780132350884", martin.Id, programming.Id,
                2008, 464,
                "Even bad code can function. But if code isn't clean, it can bring a development organization to its knees.",
                "Prentice Hall"),
            new(
                "The Pragmatic Programmer",
                "9780135957059", thomas.Id, programming.Id,
                2019, 352,
                "From journeyman to master. Tips, tricks and pragmatic approaches to modern software development.",
                "Addison-Wesley"),
            new(
                "Design Patterns: Elements of Reusable Object-Oriented Software",
                "9780201633610", gamma.Id, programming.Id,
                1994, 395,
                "The classic Gang of Four catalog of 23 software design patterns.",
                "Addison-Wesley"),
            new(
                "Refactoring: Improving the Design of Existing Code",
                "9780134757599", fowler.Id, programming.Id,
                2018, 448,
                "How to safely improve the design of existing code with proven refactorings.",
                "Addison-Wesley"),
            new(
                "Atomic Habits",
                "9780735211292", clear.Id, selfHelp.Id,
                2018, 320,
                "Tiny changes, remarkable results. A proven framework for improving every day.",
                "Avery"),
        };

        db.Books.AddRange(books);
        await db.SaveChangesAsync();

        // Give every title 2 borrowable physical copies so Rentals works out of the box.
        var copies = new List<BookCopy>();
        foreach (var b in books)
        {
            var prefix = new string(b.Title.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpper();
            copies.Add(new BookCopy(b.Id, $"{prefix}-001", "A1"));
            copies.Add(new BookCopy(b.Id, $"{prefix}-002", "A1"));
        }

        db.BookCopies.AddRange(copies);
        await db.SaveChangesAsync();
    }

    private static async Task<Category> GetOrCreateCategory(
        LibraryDbContext db, string name, string? description)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing is not null) return existing;
        var category = new Category(name, description);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task<Author> GetOrCreateAuthor(LibraryDbContext db, string name)
    {
        var existing = await db.Authors.FirstOrDefaultAsync(a => a.Name == name);
        if (existing is not null) return existing;
        var author = new Author(name);
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        return author;
    }
}
